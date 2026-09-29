using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Infrastructure.Persistence;
using ReservArte.IntegrationTests.Infrastructure;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Comprobaciones del propio montaje: si fallan, el resto de tests no demuestra
/// nada (podrían estar corriendo contra otra base o con otra configuración).
/// </summary>
[Collection(ApiCollection.Name)]
public class SmokeTests(ApiFactory factory)
{
    [Fact]
    public async Task La_API_usa_la_base_del_contenedor_y_no_la_de_desarrollo()
    {
        await using var scope = factory.CreateTenantScope(TestData.OrgA);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
        db.Database.GetConnectionString().Should().Contain("Database=reservarte_tests");
        (await db.Database.SqlQueryRaw<string>("SHOW server_version").ToListAsync())
            .Single().Should().StartWith("18.");
        (await db.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task La_configuracion_del_test_se_impone_a_los_user_secrets_del_equipo()
    {
        // Las credenciales de Google se leen al registrar servicios: si los User
        // Secrets ganaran, el esquema existiría en los equipos que las tienen.
        var schemes = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();

        (await schemes.GetSchemeAsync("Google")).Should().BeNull();
    }

    [Fact]
    public async Task El_admin_sembrado_inicia_sesion_y_lista_clientas()
    {
        var token = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);

        var result = await factory.SendAsync(HttpMethod.Get, "/api/v1/customers?pageSize=100", TestData.OrgA, token);

        result.Status.Should().Be(HttpStatusCode.OK);
        result.Body.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
