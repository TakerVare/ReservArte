using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using ReservArte.Domain.Entities;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Respuestas que se generan antes de llegar a la acción (RA-869f1k17q): los 400 de
/// model binding, el 404 de una ruta que no existe y el 405 de un método no
/// admitido. Antes salían como ProblemDetails o vacías; ahora llevan el envelope.
/// </summary>
[Collection(ApiCollection.Name)]
public class ModelBindingContractTests(ApiFactory factory)
{
    [Theory]
    [InlineData("{\"email\": \"x@y.com\", \"password\": ", "password", "InvalidJson")]
    [InlineData("{\"email\": 5, \"password\": \"x\"}", "email", "InvalidJson")]
    [InlineData("", "body", "MissingBody")]
    public async Task Un_cuerpo_que_no_se_puede_leer_da_400_con_envelope_y_el_campo_del_contrato(
        string body, string field, string code)
    {
        var result = await SendRawAsync(HttpMethod.Post, "/api/v1/auth/login", body, token: null);

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        var details = Details(result);
        details.Should().ContainSingle();
        details[0].GetProperty("field").GetString().Should().Be(field);
        details[0].GetProperty("code").GetString().Should().Be(code);
    }

    [Fact]
    public async Task Un_valor_con_el_tipo_equivocado_en_un_campo_anidado_se_senala_con_su_ruta()
    {
        var token = await AdminTokenAsync();
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA);

        var result = await SendRawAsync(HttpMethod.Put, $"/api/v1/employees/{employee.Id}/availability",
            "{\"weeklySchedule\": [{\"dayOfWeek\": \"lunes\", \"startTime\": \"09:00\", \"endTime\": \"14:00\"}]}",
            token);

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        Details(result).Select(d => d.GetProperty("field").GetString())
            .Should().ContainSingle().Which.Should().Be("weeklySchedule[0].dayOfWeek");
    }

    [Fact]
    public async Task El_mensaje_no_filtra_detalles_internos_del_parser()
    {
        var result = await SendRawAsync(HttpMethod.Post, "/api/v1/auth/login", "{\"email\": ", token: null);

        var raw = result.Body.ToString();
        raw.Should().NotContain("LineNumber").And.NotContain("BytePosition").And.NotContain("Path: $");
        raw.Should().NotContain("\"request\"");
    }

    [Fact]
    public async Task Parametros_de_consulta_no_convertibles_dan_400_con_un_detalle_por_parametro()
    {
        var token = await AdminTokenAsync();

        var result = await SendRawAsync(HttpMethod.Get, "/api/v1/customers?page=abc&isActive=xyz", body: null, token);

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.ShouldBeEnvelope(success: false);
        Details(result).Select(d => (d.GetProperty("field").GetString(), d.GetProperty("code").GetString()))
            .Should().BeEquivalentTo([("page", "InvalidFormat"), ("isActive", "InvalidFormat")]);
    }

    [Theory]
    [InlineData("/api/v1/no-existe")]
    [InlineData("/api/v1/customers/abc")]
    public async Task Una_ruta_que_no_existe_da_404_GEN_NOT_FOUND_con_envelope(string path)
    {
        var token = await AdminTokenAsync();

        var result = await SendRawAsync(HttpMethod.Get, path, body: null, token);

        result.Status.Should().Be(HttpStatusCode.NotFound);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    [Fact]
    public async Task Un_404_de_un_controlador_conserva_su_propio_mensaje()
    {
        var token = await AdminTokenAsync();

        var result = await SendRawAsync(HttpMethod.Get, "/api/v1/customers/999999", body: null, token);

        result.Status.Should().Be(HttpStatusCode.NotFound);
        result.Body.GetProperty("error").GetProperty("message").GetString()
            .Should().NotBe("No existe el recurso solicitado.");
    }

    [Fact]
    public async Task Un_metodo_no_admitido_da_405_GEN_METHOD_NOT_ALLOWED_con_envelope_y_Allow()
    {
        var token = await AdminTokenAsync();

        using var client = factory.CreateClient();
        using var request = Request(new HttpMethod("PATCH"), "/api/v1/customers", body: null, token);
        using var response = await client.SendAsync(request);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be(ErrorCodes.GenMethodNotAllowed);
        response.Content.Headers.Allow.Should().Contain(["GET", "POST"]);
    }

    [Fact]
    public async Task Fuera_de_api_un_404_no_se_toca()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/no-existe");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }

    private async Task<string> AdminTokenAsync()
    {
        var admin = await factory.CreateEmployeeAsync(TestData.OrgA, Roles.Admin);
        return await factory.TokenForAsync(TestData.OrgA, admin.Id);
    }

    /// <summary>Envío con el cuerpo tal cual (JSON roto incluido), que <c>SendAsync</c> no permite.</summary>
    private async Task<ApiResult> SendRawAsync(HttpMethod method, string path, string? body, string? token)
    {
        using var client = factory.CreateClient();
        using var request = Request(method, path, body, token);
        using var response = await client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();
        var json = string.IsNullOrWhiteSpace(raw) ? default : JsonDocument.Parse(raw).RootElement.Clone();
        return new ApiResult(response.StatusCode, json, response.Headers);
    }

    private static HttpRequestMessage Request(HttpMethod method, string path, string? body, string? token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Organization-Id", TestData.OrgA.ToString());
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static List<JsonElement> Details(ApiResult result) =>
        result.Body.GetProperty("error").GetProperty("details").EnumerateArray().ToList();
}
