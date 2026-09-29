using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Emails y búsquedas sobre PostgreSQL (H-37): el motor compara texto
/// distinguiendo mayúsculas, así que la aplicación normaliza los emails y busca
/// en minúsculas, y un CHECK impide que entre un email con mayúsculas. SQLite no
/// reproduce nada de esto.
/// </summary>
[Collection(ApiCollection.Name)]
public class EmailAndSearchTests(ApiFactory factory)
{
    [Fact]
    public async Task Un_alta_de_clienta_guarda_el_email_en_minusculas_y_sin_espacios()
    {
        var token = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);
        var email = TestData.UniqueEmail("Nueva.Clienta");

        var result = await factory.SendAsync(HttpMethod.Post, "/api/v1/customers", TestData.OrgA, token,
            NewCustomer($"  {email.ToUpperInvariant()} "));

        result.Status.Should().Be(HttpStatusCode.Created);
        result.Data.GetProperty("email").GetString().Should().Be(email.ToLowerInvariant());
        factory.Emails.Sent.Should().Contain(m => m.To == email.ToLowerInvariant());
    }

    [Fact]
    public async Task El_mismo_email_con_otras_mayusculas_en_el_mismo_centro_da_409()
    {
        var token = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);
        var email = TestData.UniqueEmail("duplicada");
        (await factory.SendAsync(HttpMethod.Post, "/api/v1/customers", TestData.OrgA, token, NewCustomer(email)))
            .Status.Should().Be(HttpStatusCode.Created);

        var result = await factory.SendAsync(HttpMethod.Post, "/api/v1/customers", TestData.OrgA, token,
            NewCustomer(email.ToUpperInvariant()));

        result.Status.Should().Be(HttpStatusCode.Conflict);
        result.ErrorCode.Should().Be(ErrorCodes.GenConflict);
    }

    [Fact]
    public async Task El_mismo_email_en_otro_centro_si_se_admite()
    {
        // El email es único por organización, no global (RA-869f1xc0u).
        var email = TestData.UniqueEmail("en.dos.centros");
        var tokenA = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);
        var tokenB = await factory.LoginAsync(TestData.AdminB, TestData.OrgB);

        var inA = await factory.SendAsync(HttpMethod.Post, "/api/v1/customers", TestData.OrgA, tokenA, NewCustomer(email));
        var inB = await factory.SendAsync(HttpMethod.Post, "/api/v1/customers", TestData.OrgB, tokenB,
            NewCustomer(email.ToUpperInvariant()));

        inA.Status.Should().Be(HttpStatusCode.Created);
        inB.Status.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Una_empleada_nueva_tambien_se_guarda_en_minusculas_y_no_admite_duplicados_por_mayusculas()
    {
        var token = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);
        var email = TestData.UniqueEmail("Nueva.Empleada");
        var body = new { firstName = "Nora", lastName = "Prueba", email = email.ToUpperInvariant(), rol = Roles.Employee };

        var created = await factory.SendAsync(HttpMethod.Post, "/api/v1/employees", TestData.OrgA, token, body);
        var duplicated = await factory.SendAsync(HttpMethod.Post, "/api/v1/employees", TestData.OrgA, token,
            body with { email = email.ToLowerInvariant() });

        created.Status.Should().Be(HttpStatusCode.Created);
        created.Data.GetProperty("email").GetString().Should().Be(email.ToLowerInvariant());
        duplicated.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task El_login_admite_el_email_con_otras_mayusculas()
    {
        // Sin caché de tokens a propósito: es el login lo que se prueba.
        var result = await factory.SendAsync(
            HttpMethod.Post, "/api/v1/auth/login", TestData.OrgB, token: null,
            new { email = TestData.AdminB.Email.ToUpperInvariant(), password = TestData.AdminB.Password });

        result.Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task La_base_rechaza_un_email_con_mayusculas_que_se_salte_la_normalizacion()
    {
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        await using var scope = factory.CreateTenantScope(TestData.OrgA);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tracked = await db.Customers.SingleAsync(c => c.Id == customer.Id);

        tracked.Email = tracked.Email.ToUpperInvariant();
        var act = () => db.SaveChangesAsync();

        var error = (await act.Should().ThrowAsync<DbUpdateException>()).Which.InnerException
            .Should().BeOfType<PostgresException>().Which;
        error.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        error.ConstraintName.Should().Be("CK_Customers_EmailLowercase");
    }

    [Fact]
    public async Task La_busqueda_de_clientas_no_distingue_mayusculas()
    {
        var token = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);
        var marker = $"Zafiro{Guid.NewGuid():N}"[..14];
        (await factory.SendAsync(HttpMethod.Post, "/api/v1/customers", TestData.OrgA, token,
            NewCustomer(TestData.UniqueEmail("busqueda"), lastName: marker)))
            .Status.Should().Be(HttpStatusCode.Created);

        var upper = await factory.SendAsync(HttpMethod.Get,
            $"/api/v1/customers?search={marker.ToUpperInvariant()}", TestData.OrgA, token);
        var lower = await factory.SendAsync(HttpMethod.Get,
            $"/api/v1/customers?search={marker.ToLowerInvariant()}", TestData.OrgA, token);

        upper.Data.GetProperty("items").GetArrayLength().Should().Be(1);
        lower.Data.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Theory]
    [InlineData("%25")]
    [InlineData("_")]
    public async Task Los_comodines_de_LIKE_se_buscan_como_texto(string search)
    {
        var token = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);

        var result = await factory.SendAsync(HttpMethod.Get,
            $"/api/v1/customers?search={search}&pageSize=100", TestData.OrgA, token);

        result.Status.Should().Be(HttpStatusCode.OK);
        result.Data.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    private static object NewCustomer(string email, string lastName = "Prueba") => new
    {
        firstName = "Clienta",
        lastName,
        email,
        phone = "+34600111222",
        grantedConsents = new[] { "data_processing" },
    };
}
