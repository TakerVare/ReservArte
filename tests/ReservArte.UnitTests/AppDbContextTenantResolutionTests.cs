using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// `AppDbContext` tiene dos constructores: uno con el tenant y otro sin él.
/// Si el contenedor eligiera el segundo, los query filters no filtrarían y el
/// aislamiento multi-tenant fallaría en silencio — sin error, devolviendo datos
/// de otras organizaciones. Este test fija que la resolución por DI, tal como
/// la monta la API, entrega el contexto CON tenant.
/// </summary>
public class AppDbContextTenantResolutionTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private readonly SqliteConnection _connection;

    public AppDbContextTenantResolutionTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    private sealed class FakeCurrentOrganization : ICurrentOrganizationService
    {
        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        // Mismo registro que ReservArte-API: el holder de tenant es scoped y el
        // DbContext se resuelve por DI.
        services.AddScoped<ICurrentOrganizationService, FakeCurrentOrganization>();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

        return services.BuildServiceProvider();
    }

    [Fact]
    public void El_contenedor_inyecta_el_tenant_en_el_DbContext()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Si DI hubiese elegido el constructor sin tenant, este campo sería null
        // y el filtro dejaría pasar todas las organizaciones.
        var field = typeof(AppDbContext).GetField(
            "_currentOrganization",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        field.Should().NotBeNull("el contexto debe guardar el holder de tenant");
        field!.GetValue(context).Should().NotBeNull(
            "AddDbContext debe resolver el constructor que recibe ICurrentOrganizationService");
    }

    [Fact]
    public async Task Resuelto_por_DI_el_query_filter_aisla_de_verdad()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var tenant = scope.ServiceProvider.GetRequiredService<ICurrentOrganizationService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.EnsureCreatedAsync();
        Seed(context);

        tenant.SetOrganization(OrgA);

        var disponibilidades = await context.EmployeeAvailabilities.ToListAsync();

        disponibilidades.Should().OnlyContain(a => a.OrganizationId == OrgA);
        disponibilidades.Should().ContainSingle();
    }

    private static void Seed(AppDbContext context)
    {
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "Org A", Subdomain = "org-a" },
            new Organization { Id = OrgB, Name = "Org B", Subdomain = "org-b" });

        context.Users.AddRange(
            NewUser(1, OrgA, "ana@orga.com"),
            NewUser(2, OrgB, "diana@orgb.com"));

        context.Employees.AddRange(
            new Employee { Id = 1, OrganizationId = OrgA, FirstName = "Ana", LastName = "A", Email = "ana@orga.com" },
            new Employee { Id = 2, OrganizationId = OrgB, FirstName = "Diana", LastName = "D", Email = "diana@orgb.com" });

        context.EmployeeAvailabilities.AddRange(
            new EmployeeAvailability
            {
                EmployeeId = 1,
                OrganizationId = OrgA,
                DayOfWeek = WeekDay.Monday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0),
            },
            new EmployeeAvailability
            {
                EmployeeId = 2,
                OrganizationId = OrgB,
                DayOfWeek = WeekDay.Monday,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(19, 0),
            });

        context.SaveChanges();
    }

    private static User NewUser(int id, Guid organizationId, string email) => new()
    {
        Id = id,
        OrganizationId = organizationId,
        FirstName = "N",
        LastName = "A",
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Rol = "employee",
        SecurityStamp = Guid.NewGuid().ToString(),
    };
}
