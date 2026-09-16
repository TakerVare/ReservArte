using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Persistence.Repositories;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Repositorio y esquema de Clientes (RA-869d7f32r) sobre SQLite en memoria:
/// hay SQL real (LIKE, paginación, includes filtrados) y restricciones de
/// esquema (índices únicos, CHECK) que un doble no ejercita.
/// </summary>
public class CustomerRepositoryTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private const int Autora = 1;
    private const int Carmen = 2;
    private const int Sofia = 3;
    private const int Alba = 4;
    private const int CarmenOtroCentro = 5;

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public CustomerRepositoryTests()
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

    private AppDbContext CreateContext(Guid? organizationId) => new(_options, new Tenant(organizationId));

    private static CustomerRepository CreateRepository(AppDbContext context, Guid? organizationId) =>
        new(context, new Tenant(organizationId));

    private static void Seed(AppDbContext context)
    {
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "More Than Brows", Subdomain = "morethanbrows" },
            new Organization { Id = OrgB, Name = "Otro Centro", Subdomain = "otrocentro" });

        context.Users.AddRange(
            NewUser(Autora, OrgA, "maria@reservarte.com", Roles.Employee),
            NewUser(Carmen, OrgA, "carmen@correo.com"),
            NewUser(Sofia, OrgA, "sofia@correo.com"),
            NewUser(Alba, OrgA, "alba@correo.com"),
            NewUser(CarmenOtroCentro, OrgB, "carmen@correo.com"));

        context.Employees.Add(new Employee
        {
            Id = Autora,
            OrganizationId = OrgA,
            FirstName = "María",
            LastName = "García",
            Email = "maria@reservarte.com",
        });

        context.Customers.AddRange(
            NewCustomer(Carmen, OrgA, "Carmen", "López", "carmen@correo.com", category: CustomerCategories.Vip),
            NewCustomer(Sofia, OrgA, "Sofía", "Ruiz", "sofia@correo.com", isBlocked: true),
            NewCustomer(Alba, OrgA, "Alba", "Álvarez", "alba@correo.com", isActive: false),
            // Mismo email que Carmen, en otro centro: es otra ficha (RA-869f1xc0u).
            NewCustomer(CarmenOtroCentro, OrgB, "Carmen", "López", "carmen@correo.com"));

        context.CustomerNotes.AddRange(
            NewNote(Carmen, "Prefiere cita por la tarde", new DateTime(2026, 9, 1)),
            NewNote(Carmen, "Pidió presupuesto de micropigmentación", new DateTime(2026, 9, 10)),
            NewNote(Carmen, "Nota retirada", new DateTime(2026, 8, 1), isActive: false));

        context.CustomerAllergies.AddRange(
            new CustomerAllergy
            {
                OrganizationId = OrgA,
                CustomerId = Carmen,
                AllergyDescription = "Látex",
                Severity = AllergySeverities.High,
            },
            new CustomerAllergy
            {
                OrganizationId = OrgA,
                CustomerId = Carmen,
                AllergyDescription = "Descartada",
                Severity = AllergySeverities.Low,
                IsActive = false,
            });

        context.CustomerConsents.AddRange(
            NewConsent(Carmen, CustomerConsentTypes.DataProcessing, isGranted: true),
            // Fila antigua dada de baja: no cuenta como vigente.
            NewConsent(Carmen, CustomerConsentTypes.Marketing, isGranted: true, isActive: false));

        context.SaveChanges();
    }

    private static User NewUser(int id, Guid organizationId, string email, string rol = Roles.Customer) => new()
    {
        Id = id,
        OrganizationId = organizationId,
        FirstName = "N",
        LastName = "A",
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Rol = rol,
        SecurityStamp = Guid.NewGuid().ToString(),
    };

    private static Customer NewCustomer(
        int id, Guid organizationId, string firstName, string lastName, string email,
        string category = CustomerCategories.Regular, bool isBlocked = false, bool isActive = true) => new()
        {
            Id = id,
            OrganizationId = organizationId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Category = category,
            IsBlocked = isBlocked,
            IsActive = isActive,
        };

    private static CustomerNote NewNote(int customerId, string note, DateTime createdAt, bool isActive = true) => new()
    {
        OrganizationId = OrgA,
        CustomerId = customerId,
        EmployeeId = Autora,
        Note = note,
        CreatedAt = createdAt,
        IsActive = isActive,
    };

    private static CustomerConsent NewConsent(
        int customerId, string type, bool isGranted, bool isActive = true, Guid? organizationId = null) => new()
        {
            OrganizationId = organizationId ?? OrgA,
            CustomerId = customerId,
            ConsentType = type,
            IsGranted = isGranted,
            GrantedAt = isGranted ? DateTime.UtcNow : null,
            IsActive = isActive,
        };

    /// <summary>Guarda en un contexto sin tenant y devuelve la excepción, si la hay.</summary>
    private async Task<Exception?> SaveAsync(Action<AppDbContext> change)
    {
        using var context = CreateContext(organizationId: null);
        change(context);
        return await Record.ExceptionAsync(() => context.SaveChangesAsync());
    }

    // ── Lista paginada ────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_sin_filtros_devuelve_los_activos_del_tenant_por_apellidos()
    {
        using var context = CreateContext(OrgA);

        var result = await CreateRepository(context, OrgA).GetPagedAsync(new CustomerFilter());

        result.Items.Select(c => c.Id).Should().Equal(Carmen, Sofia);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_filtra_por_categoria_bloqueo_y_baja()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        (await repository.GetPagedAsync(new CustomerFilter { Category = CustomerCategories.Vip }))
            .Items.Select(c => c.Id).Should().Equal(Carmen);

        (await repository.GetPagedAsync(new CustomerFilter { IsBlocked = true }))
            .Items.Select(c => c.Id).Should().Equal(Sofia);

        (await repository.GetPagedAsync(new CustomerFilter { IsBlocked = false }))
            .Items.Select(c => c.Id).Should().Equal(Carmen);

        (await repository.GetPagedAsync(new CustomerFilter { IsActive = false }))
            .Items.Select(c => c.Id).Should().Equal(Alba);
    }

    [Fact]
    public async Task GetPagedAsync_busca_en_nombre_apellidos_y_email_sin_salir_del_tenant()
    {
        using var contextA = CreateContext(OrgA);
        using var contextB = CreateContext(OrgB);

        (await CreateRepository(contextA, OrgA).GetPagedAsync(new CustomerFilter { Search = "sof" }))
            .Items.Select(c => c.Id).Should().Equal(Sofia);

        (await CreateRepository(contextA, OrgA).GetPagedAsync(new CustomerFilter { Search = "ruiz" }))
            .Items.Select(c => c.Id).Should().Equal(Sofia);

        (await CreateRepository(contextB, OrgB).GetPagedAsync(new CustomerFilter { Search = "carmen@correo" }))
            .Items.Select(c => c.Id).Should().Equal(CarmenOtroCentro);
    }

    [Fact]
    public async Task GetPagedAsync_pagina_y_cuenta_el_total_del_filtro()
    {
        using var context = CreateContext(OrgA);

        var page = await CreateRepository(context, OrgA)
            .GetPagedAsync(new CustomerFilter { Page = 2, PageSize = 1 });

        page.Items.Select(c => c.Id).Should().Equal(Sofia);
        page.TotalCount.Should().Be(2);
        page.TotalPages.Should().Be(2);
    }

    // ── Ficha, email y perfil ─────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_no_encuentra_un_cliente_de_otra_organizacion()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        (await repository.GetByIdAsync(Carmen)).Should().NotBeNull();
        (await repository.GetByIdAsync(CarmenOtroCentro)).Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_resuelve_la_ficha_de_la_organizacion_actual()
    {
        using var contextA = CreateContext(OrgA);
        using var contextB = CreateContext(OrgB);

        (await CreateRepository(contextA, OrgA).GetByEmailAsync("carmen@correo.com"))!.Id.Should().Be(Carmen);
        (await CreateRepository(contextB, OrgB).GetByEmailAsync("carmen@correo.com"))!.Id.Should().Be(CarmenOtroCentro);
        (await CreateRepository(contextB, OrgB).GetByEmailAsync("sofia@correo.com")).Should().BeNull();
    }

    [Fact]
    public async Task Sin_organizacion_resuelta_no_devuelve_nada()
    {
        // El query filter global deja pasar todo sin tenant; el repositorio no.
        using var context = CreateContext(organizationId: null);
        var repository = CreateRepository(context, organizationId: null);

        (await repository.GetPagedAsync(new CustomerFilter())).Items.Should().BeEmpty();
        (await repository.GetByIdAsync(Carmen)).Should().BeNull();
        (await repository.GetByEmailAsync("carmen@correo.com")).Should().BeNull();
    }

    [Fact]
    public async Task GetProfileAsync_carga_solo_notas_alergias_y_consentimientos_vigentes()
    {
        using var context = CreateContext(OrgA);

        var perfil = await CreateRepository(context, OrgA).GetProfileAsync(Carmen);

        perfil.Should().NotBeNull();
        perfil!.Notes.Select(n => n.Note).Should().Equal(
            "Pidió presupuesto de micropigmentación", "Prefiere cita por la tarde");
        perfil.Allergies.Should().ContainSingle().Which.AllergyDescription.Should().Be("Látex");
        perfil.Consents.Should().ContainSingle().Which.ConsentType.Should().Be(CustomerConsentTypes.DataProcessing);

        (await CreateRepository(context, OrgA).GetProfileAsync(CarmenOtroCentro)).Should().BeNull();
    }

    [Fact]
    public async Task Add_y_Update_persisten_la_ficha()
    {
        using (var context = CreateContext(OrgA))
        {
            context.Users.Add(NewUser(6, OrgA, "lucia@correo.com"));
            var repository = CreateRepository(context, OrgA);

            repository.Add(NewCustomer(6, OrgA, "Lucía", "Martínez", "lucia@correo.com"));
            await repository.SaveChangesAsync();

            var lucia = await repository.GetByIdAsync(6);
            lucia!.Category = CustomerCategories.Vip;
            repository.Update(lucia);
            await repository.SaveChangesAsync();
        }

        using var check = CreateContext(OrgA);
        var guardada = await check.Customers.SingleAsync(c => c.Id == 6);
        guardada.Category.Should().Be(CustomerCategories.Vip);
        guardada.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Las_notas_alergias_y_consentimientos_quedan_aislados_por_tenant()
    {
        using var contextB = CreateContext(OrgB);

        (await contextB.CustomerNotes.CountAsync()).Should().Be(0);
        (await contextB.CustomerAllergies.CountAsync()).Should().Be(0);
        (await contextB.CustomerConsents.CountAsync()).Should().Be(0);
    }

    // ── Restricciones del esquema ─────────────────────────────────────────

    [Fact]
    public async Task El_email_es_unico_dentro_de_la_organizacion()
    {
        // Carmen ya existe en los dos centros (sembrado); una segunda en A choca.
        var error = await SaveAsync(context =>
        {
            context.Users.Add(NewUser(6, OrgA, "otra@correo.com"));
            context.Customers.Add(NewCustomer(6, OrgA, "Otra", "Carmen", "carmen@correo.com"));
        });

        error.Should().BeOfType<DbUpdateException>();
    }

    [Theory]
    [InlineData("category")]
    [InlineData("contact")]
    [InlineData("severity")]
    [InlineData("consent")]
    public async Task Un_valor_fuera_de_catalogo_lo_rechaza_la_base_de_datos(string catalogo)
    {
        var error = await SaveAsync(context =>
        {
            switch (catalogo)
            {
                case "category":
                    context.Users.Add(NewUser(6, OrgA, "x@correo.com"));
                    context.Customers.Add(NewCustomer(6, OrgA, "X", "Y", "x@correo.com", category: "blocked"));
                    break;
                case "contact":
                    context.Users.Add(NewUser(6, OrgA, "x@correo.com"));
                    var customer = NewCustomer(6, OrgA, "X", "Y", "x@correo.com");
                    customer.PreferredContactMethod = "fax";
                    context.Customers.Add(customer);
                    break;
                case "severity":
                    context.CustomerAllergies.Add(new CustomerAllergy
                    {
                        OrganizationId = OrgA,
                        CustomerId = Sofia,
                        AllergyDescription = "X",
                        Severity = "critical",
                    });
                    break;
                case "consent":
                    context.CustomerConsents.Add(NewConsent(Sofia, "newsletter", isGranted: false));
                    break;
            }
        });

        error.Should().BeOfType<DbUpdateException>();
    }

    [Fact]
    public void Los_CHECK_de_catalogo_se_generan_desde_las_constantes_de_dominio()
    {
        using var context = CreateContext(organizationId: null);

        // Los CHECK solo viven en el modelo de diseño (el de ejecución los descarta).
        var model = context.GetService<IDesignTimeModel>().Model;

        string Check(Type entity, string name) =>
            model.FindEntityType(entity)!.GetCheckConstraints().Single(c => c.Name == name).Sql;

        Check(typeof(Customer), "CK_Customers_Category").Should()
            .Be("[Category] IN ('regular', 'vip', 'new')");
        Check(typeof(Customer), "CK_Customers_PreferredContactMethod").Should()
            .Be("[PreferredContactMethod] IN ('email', 'phone', 'sms', 'whatsapp')");
        Check(typeof(CustomerAllergy), "CK_CustomerAllergies_Severity").Should()
            .Be("[Severity] IN ('low', 'medium', 'high')");
        Check(typeof(CustomerConsent), "CK_CustomerConsents_ConsentType").Should()
            .Be("[ConsentType] IN ('data_processing', 'marketing', 'photos', 'whatsapp', 'saved_cards')");
    }

    [Fact]
    public async Task Solo_puede_haber_un_consentimiento_vigente_por_finalidad()
    {
        var duplicado = await SaveAsync(context =>
            context.CustomerConsents.Add(NewConsent(Carmen, CustomerConsentTypes.DataProcessing, isGranted: false)));

        duplicado.Should().BeOfType<DbUpdateException>();

        // Una fila dada de baja no cuenta: el marketing antiguo convive con uno nuevo.
        var nuevoMarketing = await SaveAsync(context =>
            context.CustomerConsents.Add(NewConsent(Carmen, CustomerConsentTypes.Marketing, isGranted: true)));

        nuevoMarketing.Should().BeNull();
    }

    [Fact]
    public async Task Un_consentimiento_otorgado_exige_fecha_de_otorgamiento()
    {
        var error = await SaveAsync(context =>
        {
            var consent = NewConsent(Sofia, CustomerConsentTypes.DataProcessing, isGranted: true);
            consent.GrantedAt = null;
            context.CustomerConsents.Add(consent);
        });

        error.Should().BeOfType<DbUpdateException>();
    }
}
