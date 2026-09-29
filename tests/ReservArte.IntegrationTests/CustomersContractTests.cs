using System.Net;
using AwesomeAssertions;
using ReservArte.Domain.Entities;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Contrato HTTP de <c>/api/v1/customers</c> (RA-869f2gh37): quién puede hacer qué,
/// qué código devuelve cada caso y que todas las respuestas llevan el envelope,
/// también los 401 y 403 que emite el middleware de autenticación.
/// </summary>
[Collection(ApiCollection.Name)]
public class CustomersContractTests(ApiFactory factory)
{
    private const string Customers = "/api/v1/customers";

    // ── Autenticación ────────────────────────────────────────────────────

    [Fact]
    public async Task Sin_token_da_401_con_envelope_y_WWW_Authenticate()
    {
        var result = await factory.SendAsync(HttpMethod.Get, Customers, TestData.OrgA, token: null);

        result.Status.Should().Be(HttpStatusCode.Unauthorized);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenUnauthorized);
        result.Headers.WwwAuthenticate.Should().ContainSingle(h => h.Scheme == "Bearer");
    }

    [Fact]
    public async Task Un_ticket_de_2FA_pendiente_no_sirve_como_sesion()
    {
        var admin = await factory.CreateEmployeeAsync(TestData.OrgA, Roles.Admin);
        var ticket = await factory.MfaTicketForAsync(TestData.OrgA, admin.Id);

        var result = await factory.SendAsync(HttpMethod.Get, Customers, TestData.OrgA, ticket);

        result.Status.Should().Be(HttpStatusCode.Unauthorized);
        result.ShouldBeEnvelope(success: false);
    }

    [Fact]
    public async Task Una_token_con_firma_falsa_da_401()
    {
        var admin = await factory.CreateEmployeeAsync(TestData.OrgA, Roles.Admin);
        var token = await factory.TokenForAsync(TestData.OrgA, admin.Id);
        var tampered = token[..^4] + (token[^4..] == "AAAA" ? "BBBB" : "AAAA");

        var result = await factory.SendAsync(HttpMethod.Get, Customers, TestData.OrgA, tampered);

        result.Status.Should().Be(HttpStatusCode.Unauthorized);
        result.ShouldBeEnvelope(success: false);
    }

    // ── Autorización por rol ─────────────────────────────────────────────

    [Fact]
    public async Task Una_clienta_no_accede_a_la_gestion_de_clientas()
    {
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, customer.Id);

        var result = await factory.SendAsync(HttpMethod.Get, Customers, TestData.OrgA, token);

        result.Status.Should().Be(HttpStatusCode.Forbidden);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
    }

    [Fact]
    public async Task Una_empleada_lee_la_lista_y_la_ficha()
    {
        var token = await TokenAsync(Roles.Employee);
        var target = await factory.CreateCustomerAsync(TestData.OrgA);

        var list = await factory.SendAsync(HttpMethod.Get, Customers, TestData.OrgA, token);
        var detail = await factory.SendAsync(HttpMethod.Get, $"{Customers}/{target.Id}", TestData.OrgA, token);

        list.Status.Should().Be(HttpStatusCode.OK);
        list.ShouldBeEnvelope(success: true);
        list.Body.GetProperty("meta").GetProperty("pagination").GetProperty("totalCount").GetInt32()
            .Should().BePositive();
        detail.Status.Should().Be(HttpStatusCode.OK);
        detail.ShouldBeEnvelope(success: true);
        detail.Data.GetProperty("id").GetInt32().Should().Be(target.Id);
    }

    [Fact]
    public async Task Una_empleada_no_puede_crear_editar_dar_de_baja_ni_reactivar()
    {
        var token = await TokenAsync(Roles.Employee);
        var target = await factory.CreateCustomerAsync(TestData.OrgA);

        var responses = new[]
        {
            await factory.SendAsync(HttpMethod.Post, Customers, TestData.OrgA, token, NewCustomer()),
            await factory.SendAsync(HttpMethod.Put, $"{Customers}/{target.Id}", TestData.OrgA, token,
                UpdateOf(target.Email)),
            await factory.SendAsync(HttpMethod.Delete, $"{Customers}/{target.Id}", TestData.OrgA, token),
            await factory.SendAsync(HttpMethod.Post, $"{Customers}/{target.Id}/reactivate", TestData.OrgA, token),
        };

        responses.Should().AllSatisfy(r =>
        {
            r.Status.Should().Be(HttpStatusCode.Forbidden);
            r.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
        });
    }

    [Theory]
    [InlineData(Roles.Manager)]
    [InlineData(Roles.Admin)]
    public async Task La_gerencia_crea_edita_da_de_baja_y_reactiva(string rol)
    {
        var token = await TokenAsync(rol);

        var created = await factory.SendAsync(HttpMethod.Post, Customers, TestData.OrgA, token, NewCustomer());
        created.Status.Should().Be(HttpStatusCode.Created);
        created.ShouldBeEnvelope(success: true);
        var id = created.Data.GetProperty("id").GetInt32();
        var email = created.Data.GetProperty("email").GetString()!;
        created.Headers.Location!.AbsolutePath.Should().Be($"{Customers}/{id}");

        var updated = await factory.SendAsync(HttpMethod.Put, $"{Customers}/{id}", TestData.OrgA, token,
            UpdateOf(email, firstName: "Editada"));
        updated.Status.Should().Be(HttpStatusCode.OK);
        updated.Data.GetProperty("firstName").GetString().Should().Be("Editada");

        var deactivated = await factory.SendAsync(HttpMethod.Delete, $"{Customers}/{id}", TestData.OrgA, token);
        deactivated.Status.Should().Be(HttpStatusCode.OK);
        deactivated.Data.GetProperty("isActive").GetBoolean().Should().BeFalse();

        var reactivated = await factory.SendAsync(HttpMethod.Post, $"{Customers}/{id}/reactivate", TestData.OrgA, token);
        reactivated.Status.Should().Be(HttpStatusCode.OK);
        reactivated.Data.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    // ── Errores de negocio ───────────────────────────────────────────────

    [Fact]
    public async Task Un_alta_invalida_da_400_con_los_campos_en_camelCase()
    {
        var token = await TokenAsync(Roles.Manager);

        var result = await factory.SendAsync(HttpMethod.Post, Customers, TestData.OrgA, token,
            new { firstName = "", lastName = "Prueba", email = "no-es-un-email", grantedConsents = new[] { "data_processing" } });

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        result.Body.GetProperty("error").GetProperty("details").EnumerateArray()
            .Select(d => d.GetProperty("field").GetString())
            .Should().Contain(["firstName", "email"]);
    }

    [Fact]
    public async Task Una_ficha_que_no_existe_da_404_con_envelope()
    {
        var token = await TokenAsync(Roles.Employee);

        var result = await factory.SendAsync(HttpMethod.Get, $"{Customers}/999999", TestData.OrgA, token);

        result.Status.Should().Be(HttpStatusCode.NotFound);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    [Fact]
    public async Task Editar_una_clienta_con_el_email_de_otra_da_409()
    {
        var token = await TokenAsync(Roles.Manager);
        var first = await factory.CreateCustomerAsync(TestData.OrgA);
        var second = await factory.CreateCustomerAsync(TestData.OrgA);

        var result = await factory.SendAsync(HttpMethod.Put, $"{Customers}/{second.Id}", TestData.OrgA, token,
            UpdateOf(first.Email.ToUpperInvariant()));

        result.Status.Should().Be(HttpStatusCode.Conflict);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenConflict);
    }

    /// <summary>Token de una cuenta nueva del centro A con ese rol.</summary>
    private async Task<string> TokenAsync(string rol)
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA, rol);
        return await factory.TokenForAsync(TestData.OrgA, employee.Id);
    }

    private static object NewCustomer() => new
    {
        firstName = "Clienta",
        lastName = "Contrato",
        email = TestData.UniqueEmail("contrato"),
        phone = "+34600111222",
        grantedConsents = new[] { "data_processing" },
    };

    private static object UpdateOf(string email, string firstName = "Clienta") => new
    {
        firstName,
        lastName = "Contrato",
        email,
        phone = "+34600111222",
        category = CustomerCategories.New,
        preferredContactMethod = CustomerContactMethods.Email,
    };
}
