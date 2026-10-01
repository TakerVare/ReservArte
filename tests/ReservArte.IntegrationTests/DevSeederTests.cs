using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Persistence.Seeders;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Administrador que entra solo con Google (RA-869faedz3): lo crea DevSeeder,
/// también en bases ya sembradas, sin contraseña local.
/// </summary>
[Collection(ApiCollection.Name)]
public class DevSeederTests(ApiFactory factory)
{
    private const string GoogleAdminEmail = "takervare@gmail.com";

    [Fact]
    public async Task El_admin_de_Google_existe_en_el_centro_piloto_sin_contrasena_local()
    {
        await using var scope = factory.CreateTenantScope(TestData.OrgA);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var admin = await db.Users.SingleAsync(u => u.NormalizedEmail == GoogleAdminEmail.ToUpperInvariant());

        admin.OrganizationId.Should().Be(TestData.OrgA);
        admin.Rol.Should().Be(Roles.Admin);
        admin.PasswordHash.Should().BeNull();
        admin.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task En_una_base_ya_sembrada_sin_la_cuenta_la_crea_una_sola_vez()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var organizationsBefore = await db.Organizations.IgnoreQueryFilters().CountAsync();

        // Como las bases de desarrollo de antes de RA-869faedz3: organización sembrada, sin la cuenta.
        await db.Users.IgnoreQueryFilters()
            .Where(u => u.NormalizedEmail == GoogleAdminEmail.ToUpperInvariant())
            .ExecuteDeleteAsync();

        await DevSeeder.SeedAsync(db, userManager);
        await DevSeeder.SeedAsync(db, userManager);

        (await db.Users.IgnoreQueryFilters()
            .CountAsync(u => u.NormalizedEmail == GoogleAdminEmail.ToUpperInvariant()))
            .Should().Be(1);
        (await db.Organizations.IgnoreQueryFilters().CountAsync()).Should().Be(organizationsBefore);
    }

    [Fact]
    public async Task Si_ya_entro_con_Google_como_clienta_pasa_a_Admin_conserva_el_vinculo_y_da_de_baja_la_ficha()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var admin = await db.Users.IgnoreQueryFilters()
            .SingleAsync(u => u.NormalizedEmail == GoogleAdminEmail.ToUpperInvariant());

        // Como la base del Mac: la cuenta llegó por el alta social, como clienta con ficha y vínculo.
        admin.Rol = Roles.Customer;
        db.Customers.Add(new Customer
        {
            Id = admin.Id,
            OrganizationId = TestData.OrgA,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            Email = GoogleAdminEmail,
            Category = CustomerCategories.Regular,
            PreferredContactMethod = CustomerContactMethods.Email,
        });
        db.UserLogins.Add(new UserLogin
        {
            LoginProvider = "Google",
            ProviderKey = "google-sub-takervare",
            ProviderDisplayName = "Google",
            UserId = admin.Id,
            OrganizationId = TestData.OrgA,
        });
        await db.SaveChangesAsync();
        var userId = admin.Id;

        await DevSeeder.SeedAsync(db, userManager);

        db.ChangeTracker.Clear();
        var promoted = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == userId);
        promoted.Rol.Should().Be(Roles.Admin);
        (await db.Customers.IgnoreQueryFilters().SingleAsync(c => c.Id == userId)).IsActive.Should().BeFalse();
        (await db.UserLogins.IgnoreQueryFilters().CountAsync(l => l.UserId == userId && l.LoginProvider == "Google"))
            .Should().Be(1);
    }

    [Fact]
    public async Task Sin_contrasena_local_el_login_con_contrasena_da_la_respuesta_opaca()
    {
        var result = await factory.SendAsync(HttpMethod.Post, "/api/v1/auth/login", TestData.OrgA, token: null,
            new { email = GoogleAdminEmail, password = "Cualquiera-123" });

        result.Status.Should().Be(HttpStatusCode.Unauthorized);
        result.ErrorCode.Should().Be(ErrorCodes.AuthInvalidCredentials);
    }
}
