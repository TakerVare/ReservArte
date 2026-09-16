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
/// Repositorio y esquema del catálogo de servicios (RA-869d7f3z0) sobre SQLite
/// en memoria: hay SQL real (LIKE, paginación, includes filtrados) y
/// restricciones de esquema (índice único filtrado, CHECK de EmployeeLevels)
/// que un doble no ejercita.
/// </summary>
public class ServiceRepositoryTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private const int Cejas = 1;
    private const int Pestanas = 2;
    private const int CategoriaOtroCentro = 3;

    private const int Diseno = 1;
    private const int Tinte = 2;
    private const int Retirado = 3;
    private const int ServicioOtroCentro = 4;

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public ServiceRepositoryTests()
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

    private static ServiceRepository CreateRepository(AppDbContext context, Guid? organizationId) =>
        new(context, new Tenant(organizationId));

    private static void Seed(AppDbContext context)
    {
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "More Than Brows", Subdomain = "morethanbrows" },
            new Organization { Id = OrgB, Name = "Otro Centro", Subdomain = "otrocentro" });

        context.ServiceCategories.AddRange(
            new ServiceCategory { Id = Cejas, OrganizationId = OrgA, Name = "Cejas", DisplayOrder = 1 },
            new ServiceCategory { Id = Pestanas, OrganizationId = OrgA, Name = "Pestañas", DisplayOrder = 0 },
            new ServiceCategory { Id = CategoriaOtroCentro, OrganizationId = OrgB, Name = "Uñas", DisplayOrder = 0 });

        context.Services.AddRange(
            NewService(Diseno, OrgA, Cejas, "Diseño de cejas", "Con medición", 45, 25.00m),
            NewService(Tinte, OrgA, Cejas, "Tinte de cejas", "Semipermanente", 30, 18.00m),
            NewService(Retirado, OrgA, Pestanas, "Lifting antiguo", null, 60, 40.00m, isActive: false),
            NewService(ServicioOtroCentro, OrgB, CategoriaOtroCentro, "Manicura", null, 50, 20.00m));

        context.ServiceVariations.AddRange(
            new ServiceVariation
            {
                OrganizationId = OrgA,
                ServiceId = Diseno,
                Name = "Con hilo",
                PriceModifier = 5.00m,
                DurationModifier = 15,
            },
            new ServiceVariation
            {
                OrganizationId = OrgA,
                ServiceId = Diseno,
                Name = "Variación retirada",
                PriceModifier = 0m,
                DurationModifier = 0,
                IsActive = false,
            });

        context.ServicePricings.AddRange(
            new ServicePricing
            {
                OrganizationId = OrgA,
                ServiceId = Diseno,
                EmployeeLevel = EmployeeLevels.Senior,
                Price = 25.00m,
            },
            new ServicePricing
            {
                OrganizationId = OrgA,
                ServiceId = Diseno,
                EmployeeLevel = EmployeeLevels.Expert,
                Price = 30.00m,
                IsActive = false,
            });

        context.SaveChanges();
    }

    private static Service NewService(
        int id, Guid organizationId, int categoryId, string name,
        string? description, int duration, decimal price, bool isActive = true) => new()
        {
            Id = id,
            OrganizationId = organizationId,
            CategoryId = categoryId,
            Name = name,
            Description = description,
            DurationMinutes = duration,
            BasePrice = price,
            IsActive = isActive,
        };

    // ── Aislamiento por tenant ────────────────────────────────────────────

    [Fact]
    public async Task La_lista_no_devuelve_servicios_de_otra_organizacion()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var page = await repository.GetPagedAsync(new ServiceFilter());

        page.Items.Should().OnlyContain(s => s.OrganizationId == OrgA);
        page.Items.Should().NotContain(s => s.Name == "Manicura");
    }

    [Fact]
    public async Task Un_servicio_de_otra_organizacion_no_se_encuentra_por_id()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        (await repository.GetByIdAsync(ServicioOtroCentro)).Should().BeNull();
        (await repository.GetDetailAsync(ServicioOtroCentro)).Should().BeNull();
    }

    [Fact]
    public async Task Sin_organizacion_resuelta_el_repositorio_no_devuelve_nada()
    {
        // El query filter global deja pasar todo sin tenant (lo necesitan
        // migraciones y seeders); el repositorio prefiere no devolver nada.
        using var context = CreateContext(organizationId: null);
        var repository = CreateRepository(context, organizationId: null);

        (await repository.GetPagedAsync(new ServiceFilter())).Items.Should().BeEmpty();
        (await repository.GetByIdAsync(Diseno)).Should().BeNull();
        (await repository.GetCategoriesAsync()).Should().BeEmpty();
    }

    // ── Filtros y paginación ──────────────────────────────────────────────

    [Fact]
    public async Task Por_defecto_la_lista_solo_trae_los_activos()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var page = await repository.GetPagedAsync(new ServiceFilter());

        page.Items.Should().OnlyContain(s => s.IsActive);
        page.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Se_pueden_pedir_explicitamente_los_dados_de_baja()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var page = await repository.GetPagedAsync(new ServiceFilter { IsActive = false });

        page.Items.Should().ContainSingle().Which.Name.Should().Be("Lifting antiguo");
    }

    [Fact]
    public async Task La_busqueda_mira_nombre_y_descripcion()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var porNombre = await repository.GetPagedAsync(new ServiceFilter { Search = "tinte" });
        var porDescripcion = await repository.GetPagedAsync(new ServiceFilter { Search = "medición" });

        porNombre.Items.Should().ContainSingle().Which.Id.Should().Be(Tinte);
        porDescripcion.Items.Should().ContainSingle().Which.Id.Should().Be(Diseno);
    }

    [Fact]
    public async Task La_busqueda_no_revienta_con_servicios_sin_descripcion()
    {
        // El servicio retirado tiene Description null: sin la guarda del null,
        // el LIKE lo dejaría fuera o lanzaría según el proveedor.
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var page = await repository.GetPagedAsync(
            new ServiceFilter { Search = "lifting", IsActive = false });

        page.Items.Should().ContainSingle().Which.Id.Should().Be(Retirado);
    }

    [Fact]
    public async Task Se_filtra_por_categoria()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var page = await repository.GetPagedAsync(new ServiceFilter { CategoryId = Cejas });

        page.Items.Should().HaveCount(2).And.OnlyContain(s => s.CategoryId == Cejas);
    }

    [Fact]
    public async Task El_total_es_el_del_filtro_y_no_el_de_la_pagina()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var page = await repository.GetPagedAsync(new ServiceFilter { Page = 1, PageSize = 1 });

        page.Items.Should().ContainSingle();
        page.TotalCount.Should().Be(2);
        page.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task El_tamano_de_pagina_esta_acotado()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var page = await repository.GetPagedAsync(new ServiceFilter { Page = 0, PageSize = 5000 });

        // Página mínima 1 y tamaño máximo 100: un pageSize enorme no debe poder
        // arrastrar el catálogo entero de una vez.
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(100);
    }

    // ── Detalle ───────────────────────────────────────────────────────────

    [Fact]
    public async Task El_detalle_carga_solo_variaciones_y_tarifas_vigentes()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var service = await repository.GetDetailAsync(Diseno);

        service!.Category!.Name.Should().Be("Cejas");
        service.Variations.Should().ContainSingle().Which.Name.Should().Be("Con hilo");
        service.Pricings.Should().ContainSingle().Which.EmployeeLevel.Should().Be(EmployeeLevels.Senior);
    }

    // ── Categorías ────────────────────────────────────────────────────────

    [Fact]
    public async Task Las_categorias_salen_en_su_orden_de_presentacion()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var categories = await repository.GetCategoriesAsync();

        categories.Select(c => c.Name).Should().Equal("Pestañas", "Cejas");
    }

    [Fact]
    public async Task Una_categoria_de_otra_organizacion_no_se_encuentra()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        (await repository.GetCategoryByIdAsync(CategoriaOtroCentro)).Should().BeNull();
    }

    // ── Tarifas ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Solo_se_devuelve_la_tarifa_vigente_del_nivel()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var senior = await repository.GetPricingAsync(Diseno, EmployeeLevels.Senior);
        var expert = await repository.GetPricingAsync(Diseno, EmployeeLevels.Expert);

        senior!.Price.Should().Be(25.00m);
        // La de expert está retirada: el índice único solo cubre las activas, así
        // que no debe estorbar para crear otra.
        expert.Should().BeNull();
    }

    // ── Esquema ───────────────────────────────────────────────────────────

    [Fact]
    public async Task No_caben_dos_tarifas_vigentes_del_mismo_nivel()
    {
        using var context = CreateContext(organizationId: null);

        context.ServicePricings.Add(new ServicePricing
        {
            OrganizationId = OrgA,
            ServiceId = Diseno,
            EmployeeLevel = EmployeeLevels.Senior,
            Price = 99.00m,
        });

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "el índice único filtrado impide dos tarifas vigentes para el mismo nivel");
    }

    [Fact]
    public async Task El_CHECK_rechaza_un_nivel_fuera_del_catalogo()
    {
        using var context = CreateContext(organizationId: null);

        context.ServicePricings.Add(new ServicePricing
        {
            OrganizationId = OrgA,
            ServiceId = Tinte,
            EmployeeLevel = "maestro",
            Price = 10.00m,
        });

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "el CHECK se genera desde EmployeeLevels y no admite otros valores");
    }

    [Fact]
    public async Task El_CHECK_rechaza_una_duracion_no_positiva()
    {
        using var context = CreateContext(organizationId: null);

        context.Services.Add(NewService(99, OrgA, Cejas, "Servicio imposible", null, 0, 10.00m));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "un servicio de duración 0 no ocuparía hueco en la agenda");
    }
}
