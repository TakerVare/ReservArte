using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence;
using ReservArte.IntegrationTests.Infrastructure;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Filtros de tenant cerrados por defecto (869f6r5vy), contra PostgreSQL: un proceso
/// sin petición (como el futuro job de recordatorios de Hangfire) no ve datos hasta
/// fijar el tenant, y solo el ámbito de sistema ve varias organizaciones.
/// </summary>
[Collection(ApiCollection.Name)]
public class SystemScopeTests(ApiFactory factory)
{
    [Fact]
    public async Task Un_proceso_sin_tenant_no_ve_ninguna_fila_de_ningun_centro()
    {
        var customerA = await factory.CreateCustomerAsync(TestData.OrgA);
        var customerB = await factory.CreateCustomerAsync(TestData.OrgB);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        (await db.Customers.AnyAsync(c => c.Id == customerA.Id || c.Id == customerB.Id)).Should().BeFalse();
        (await db.Users.AnyAsync()).Should().BeFalse();
        (await db.Appointments.AnyAsync()).Should().BeFalse();
        (await userManager.FindByEmailAsync(customerA.Email)).Should().BeNull();
    }

    [Fact]
    public async Task El_job_fija_el_tenant_de_cada_cita_y_solo_ve_su_centro()
    {
        var customerA = await factory.CreateCustomerAsync(TestData.OrgA);
        var customerB = await factory.CreateCustomerAsync(TestData.OrgB);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Como hará el job: localiza lo pendiente en el ámbito de sistema…
        List<(int Id, Guid Org)> pending;
        using (db.EnterSystemScope("test: localizar trabajo de varios centros"))
        {
            pending = await db.Customers
                .Where(c => c.Id == customerA.Id || c.Id == customerB.Id)
                .Select(c => new ValueTuple<int, Guid>(c.Id, c.OrganizationId))
                .ToListAsync();
        }

        pending.Should().HaveCount(2);

        // …y después trabaja con el tenant de cada elemento fijado.
        scope.ServiceProvider.GetRequiredService<ICurrentOrganizationService>().SetOrganization(TestData.OrgB);
        (await db.Customers.Where(c => c.Id == customerA.Id || c.Id == customerB.Id).Select(c => c.Id).ToListAsync())
            .Should().Equal(customerB.Id);
    }
}
