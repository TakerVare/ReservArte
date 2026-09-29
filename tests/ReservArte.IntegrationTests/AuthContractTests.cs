using System.Net;
using AwesomeAssertions;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Contrato HTTP de <c>/api/v1/auth</c> (RA-869f6r81n): el mapa único de códigos
/// y la unificación de <c>AuthResult</c> con <c>Result</c> no deben cambiar ningún
/// status. El registro no pasa por el límite de login, así que el flujo completo
/// (registro, refresco y rotación) sale de una cuenta nueva.
/// </summary>
[Collection(ApiCollection.Name)]
public class AuthContractTests(ApiFactory factory)
{
    private const string Auth = "/api/v1/auth";

    [Fact]
    public async Task Registro_refresco_y_rotacion_del_refresh_token()
    {
        var registered = await Register(TestData.UniqueEmail("registro"));
        registered.Status.Should().Be(HttpStatusCode.OK);
        registered.ShouldBeEnvelope(success: true);
        var refresh = registered.Data.GetProperty("refreshToken").GetString()!;
        registered.Data.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();

        var renewed = await factory.SendAsync(HttpMethod.Post, $"{Auth}/refresh-token", TestData.OrgA, token: null,
            new { refreshToken = refresh });
        var reused = await factory.SendAsync(HttpMethod.Post, $"{Auth}/refresh-token", TestData.OrgA, token: null,
            new { refreshToken = refresh });

        renewed.Status.Should().Be(HttpStatusCode.OK);
        renewed.Data.GetProperty("refreshToken").GetString().Should().NotBe(refresh);
        reused.Status.Should().Be(HttpStatusCode.Unauthorized);
        reused.ShouldBeEnvelope(success: false);
        reused.ErrorCode.Should().Be(ErrorCodes.AuthRefreshInvalid);
    }

    [Fact]
    public async Task Un_registro_con_un_email_ya_usado_da_409()
    {
        var email = TestData.UniqueEmail("repetida");
        (await Register(email)).Status.Should().Be(HttpStatusCode.OK);

        var result = await Register(email.ToUpperInvariant());

        result.Status.Should().Be(HttpStatusCode.Conflict);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenConflict);
    }

    [Fact]
    public async Task Un_registro_invalido_da_400_con_los_campos_en_camelCase()
    {
        var result = await factory.SendAsync(HttpMethod.Post, $"{Auth}/register", TestData.OrgA, token: null,
            new { email = "mal", password = "corta", firstName = "", lastName = "Prueba" });

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        result.Body.GetProperty("error").GetProperty("details").EnumerateArray()
            .Select(d => d.GetProperty("field").GetString())
            .Should().Contain(["email", "password", "firstName", "acceptedTerms", "acceptedPrivacy"]);
    }

    [Fact]
    public async Task Un_login_con_la_contrasena_incorrecta_da_401_AUTH_INVALID_CREDENTIALS()
    {
        var result = await factory.SendAsync(HttpMethod.Post, $"{Auth}/login", TestData.OrgA, token: null,
            new { email = TestData.AdminA.Email, password = "No-Es-La-Buena-1" });

        result.Status.Should().Be(HttpStatusCode.Unauthorized);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.AuthInvalidCredentials);
    }

    [Fact]
    public async Task Un_refresh_token_inventado_da_401_AUTH_REFRESH_INVALID()
    {
        var result = await factory.SendAsync(HttpMethod.Post, $"{Auth}/refresh-token", TestData.OrgA, token: null,
            new { refreshToken = "no-existe" });

        result.Status.Should().Be(HttpStatusCode.Unauthorized);
        result.ErrorCode.Should().Be(ErrorCodes.AuthRefreshInvalid);
    }

    private Task<ApiResult> Register(string email) =>
        factory.SendAsync(HttpMethod.Post, $"{Auth}/register", TestData.OrgA, token: null, new
        {
            email,
            password = "Registro123!",
            firstName = "Nueva",
            lastName = "Registrada",
            acceptedTerms = true,
            acceptedPrivacy = true,
            acceptedTermsVersion = "1.0",
            acceptedPrivacyVersion = "1.0",
            acceptedDataProcessing = true,
        });
}
