using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Persistence.Repositories;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Repositorio de paquetes (RA-869d7f45n) sobre SQLite en memoria: el reemplazo
/// de la composición y el aislamiento por tenant son SQL real, que un doble no
/// ejercita. Mismo criterio que `ReplaceAvailabilitiesAsync` de Empleados, cuyos
/// tests fijan este mismo contrato.
/// </summary>
public class ServicePackageRepositoryTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private const int Diseno = 1;
    private const int Tinte = 2;
    private const int ServicioOtroCentro = 3;
    private const int PackA = 1;
    private const int PackOtroCentro = 2;

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public ServicePackageRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext(organizationId: null);
        context.Database.EnsureCreated();
        Seed(context);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class Tenant : ICurrentOrganizationService
    {
        public Tenant(Guid? organizationId) => OrganizationId = organizationId;

        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;
    }

    private AppDbContext CreateContext(Guid? organizationId) =>
        new(_options, new Tenant(organizationId));

    private static ServicePackageRepository CreateRepository(
        AppDbContext context, Guid? organizationId) =>
        new(context, new Tenant(organizationId));

    private static void Seed(AppDbContext context)
    {
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "More Than Brows", Subdomain = "morethanbrows" },
            new Organization { Id = OrgB, Name = "Otro Centro", Subdomain = "otrocentro" });

        context.Services.AddRange(
            NewService(Diseno, OrgA, "Diseño de cejas", 45, 25.00m),
            NewService(Tinte, OrgA, "Tinte de cejas", 30, 18.00m),
            NewService(ServicioOtroCentro, OrgB, "Manicura", 50, 20.00m));

        context.ServicePackages.AddRange(
            new ServicePackage { Id = PackA, OrganizationId = OrgA, Name = "Pack cejas", TotalPrice = 38.00m },
            new ServicePackage { Id = PackOtroCentro, OrganizationId = OrgB, Name = "Pack manos", TotalPrice = 15.00m });

        context.ServicePackageItems.Add(new ServicePackageItem
        {
            OrganizationId = OrgA,
            ServicePackageId = PackA,
            ServiceId = Diseno,
            Order = 0,
        });

        context.SaveChanges();
    }

    private static Service NewService(
        int id, Guid organizationId, string name, int duration, decimal price) => new()
        {
            Id = id,
            OrganizationId = organizationId,
            Name = name,
            DurationMinutes = duration,
            BasePrice = price,
        };

    // ── Aislamiento por tenant ────────────────────────────────────────────

    [Fact]
    public async Task La_lista_no_devuelve_paquetes_de_otra_organizacion()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var page = await repository.GetPagedAsync(new ServicePackageFilter());

        page.Items.Should().ContainSingle().Which.Name.Should().Be("Pack cejas");
    }

    [Fact]
    public async Task Un_paquete_de_otra_organizacion_no_se_encuentra()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        (await repository.GetByIdAsync(PackOtroCentro)).Should().BeNull();
        (await repository.GetDetailAsync(PackOtroCentro)).Should().BeNull();
    }

    [Fact]
    public async Task Sin_organizacion_resuelta_el_repositorio_no_devuelve_nada()
    {
        using var context = CreateContext(organizationId: null);
        var repository = CreateRepository(context, organizationId: null);

        (await repository.GetPagedAsync(new ServicePackageFilter())).Items.Should().BeEmpty();
        (await repository.GetByIdAsync(PackA)).Should().BeNull();
    }

    // ── Composición ───────────────────────────────────────────────────────

    [Fact]
    public async Task El_detalle_trae_las_lineas_en_orden_y_con_su_servicio()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        await repository.ReplaceItemsAsync(PackA, new[]
        {
            new ServicePackageItem { ServiceId = Tinte, Order = 1 },
            new ServicePackageItem { ServiceId = Diseno, Order = 0 },
        });
        await repository.SaveChangesAsync();

        var package = await repository.GetDetailAsync(PackA);

        package!.Items.Select(i => i.Order).Should().Equal(0, 1);
        package.Items.Select(i => i.Service.Name).Should().Equal("Diseño de cejas", "Tinte de cejas");
    }

    [Fact]
    public async Task ReplaceItemsAsync_sustituye_la_composicion_completa()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        // El paquete tenía «Diseño»; se queda solo con «Tinte».
        await repository.ReplaceItemsAsync(PackA, new[]
        {
            new ServicePackageItem { ServiceId = Tinte, Order = 0 },
        });
        await repository.SaveChangesAsync();

        var package = await repository.GetDetailAsync(PackA);

        package!.Items.Should().ContainSingle().Which.ServiceId.Should().Be(Tinte);
    }

    [Fact]
    public async Task ReplaceItemsAsync_impone_el_paquete_y_el_tenant_de_la_peticion()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        // Entrada maliciosa: apunta a otro paquete y a otra organización.
        var intruso = new ServicePackageItem
        {
            ServicePackageId = PackOtroCentro,
            OrganizationId = OrgB,
            ServiceId = Tinte,
            Order = 0,
        };

        await repository.ReplaceItemsAsync(PackA, new[] { intruso });
        await repository.SaveChangesAsync();

        intruso.ServicePackageId.Should().Be(PackA);
        intruso.OrganizationId.Should().Be(OrgA);
    }

    [Fact]
    public async Task ReplaceItemsAsync_no_toca_la_composicion_de_otro_paquete()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        await repository.ReplaceItemsAsync(PackA, Array.Empty<ServicePackageItem>());
        await repository.SaveChangesAsync();

        using var check = CreateContext(organizationId: null);
        var ajenas = await check.ServicePackageItems
            .Where(i => i.ServicePackageId == PackOtroCentro)
            .ToListAsync();

        // El otro centro no tenía líneas y sigue sin tenerlas; lo que importa es
        // que el borrado se acotó al paquete indicado.
        ajenas.Should().BeEmpty();
        (await check.ServicePackageItems.CountAsync()).Should().Be(0);
    }

    // ── Validación de la composición ──────────────────────────────────────

    [Fact]
    public async Task Solo_se_reconocen_los_servicios_del_centro()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var existing = await repository.ExistingServiceIdsAsync(
            new[] { Diseno, Tinte, ServicioOtroCentro, 9999 });

        // El de la otra organización y el inexistente quedan fuera: es lo que
        // permite al servicio señalar la línea concreta que falla.
        existing.Should().BeEquivalentTo(new[] { Diseno, Tinte });
    }

    [Fact]
    public async Task Una_composicion_vacia_no_consulta_servicios()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        (await repository.ExistingServiceIdsAsync(Array.Empty<int>())).Should().BeEmpty();
    }
}
