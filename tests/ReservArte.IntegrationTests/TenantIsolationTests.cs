using System.Net;
using AwesomeAssertions;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Aislamiento entre centros por HTTP, con la API entera y PostgreSQL real: el
/// tenant sale de la cabecera y del token, el filtro global y los repositorios
/// hacen el resto. Centro A = More Than Brows (DevSeeder); centro B = fixture.
/// </summary>
[Collection(ApiCollection.Name)]
public class TenantIsolationTests(ApiFactory factory)
{
    [Fact]
    public async Task La_lista_de_clientas_del_centro_A_no_trae_ninguna_del_centro_B()
    {
        var token = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);

        var result = await factory.SendAsync(HttpMethod.Get, "/api/v1/customers?pageSize=100", TestData.OrgA, token);

        result.Status.Should().Be(HttpStatusCode.OK);
        var emails = Emails(result);
        emails.Should().Contain("carmen.lopez@example.com");
        emails.Should().NotContain(TestData.CustomerB.Email);
    }

    [Fact]
    public async Task La_lista_de_clientas_del_centro_B_no_trae_ninguna_del_centro_A()
    {
        var token = await factory.LoginAsync(TestData.AdminB, TestData.OrgB);

        var result = await factory.SendAsync(HttpMethod.Get, "/api/v1/customers?pageSize=100", TestData.OrgB, token);

        result.Status.Should().Be(HttpStatusCode.OK);
        var emails = Emails(result);
        emails.Should().Contain(TestData.CustomerB.Email);
        emails.Should().NotContain("carmen.lopez@example.com");
    }

    [Fact]
    public async Task La_ficha_de_una_clienta_de_otro_centro_da_404_y_no_403()
    {
        // 404 y no 403: responder «prohibido» confirmaría que el Id existe en otro centro.
        var customerB = await factory.CreateCustomerAsync(TestData.OrgB);
        var token = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);

        var result = await factory.SendAsync(HttpMethod.Get, $"/api/v1/customers/{customerB.Id}", TestData.OrgA, token);

        result.Status.Should().Be(HttpStatusCode.NotFound);
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    [Fact]
    public async Task Un_token_del_centro_A_con_la_cabecera_del_centro_B_da_403_ORG_TENANT_MISMATCH()
    {
        var token = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);

        var result = await factory.SendAsync(HttpMethod.Get, "/api/v1/customers", TestData.OrgB, token);

        result.Status.Should().Be(HttpStatusCode.Forbidden);
        result.ErrorCode.Should().Be(ErrorCodes.OrgTenantMismatch);
    }

    [Fact]
    public async Task Sin_token_la_lista_de_clientas_da_401()
    {
        var result = await factory.SendAsync(HttpMethod.Get, "/api/v1/customers", TestData.OrgA, token: null);

        result.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Las_credenciales_de_un_centro_no_sirven_para_entrar_en_otro()
    {
        // Mismo email y contraseña, otro centro: la cuenta no existe allí.
        var result = await factory.SendAsync(
            HttpMethod.Post, "/api/v1/auth/login", TestData.OrgA, token: null,
            new { email = TestData.AdminB.Email, password = TestData.AdminB.Password });

        result.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static List<string> Emails(ApiResult result) =>
        result.Data.GetProperty("items").EnumerateArray()
            .Select(c => c.GetProperty("email").GetString()!)
            .ToList();
}
