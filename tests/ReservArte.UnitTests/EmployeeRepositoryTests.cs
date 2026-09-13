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
/// Tests del repositorio de empleados (RA-869d7ezv0) y del aislamiento
/// multi-tenant de las tablas de disponibilidad (RA-869f17myx).
///
/// Se ejecutan sobre SQLite en memoria y no sobre el proveedor InMemory: aquí
/// hay SQL real (LIKE, ORDER BY, paginación) y el doble falso no lo ejercita.
/// </summary>
public class EmployeeRepositoryTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public EmployeeRepositoryTests()
    {
        // La conexión se mantiene abierta durante todo el test: al cerrarla,
        // SQLite descarta la base en memoria.
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

    /// <summary>Doble del holder de tenant que rellena TenantMiddleware.</summary>
    private sealed class FakeCurrentOrganization : ICurrentOrganizationService
    {
        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;

        public static FakeCurrentOrganization For(Guid? organizationId)
        {
            var fake = new FakeCurrentOrganization();
            if (organizationId is { } id)
            {
                fake.SetOrganization(id);
            }

            return fake;
        }
    }

    private AppDbContext CreateContext(Guid? organizationId) =>
        new(_options, FakeCurrentOrganization.For(organizationId));

    private EmployeeRepository CreateRepository(AppDbContext context, Guid? organizationId) =>
        new(context, FakeCurrentOrganization.For(organizationId));

    private static void Seed(AppDbContext context)
    {
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "More Than Brows", Subdomain = "morethanbrows" },
            new Organization { Id = OrgB, Name = "Otro Centro", Subdomain = "otrocentro" });

        // Employee.Id es FK a AspNetUsers.Id (clave primaria compartida): cada
        // empleada necesita su usuario antes de existir como empleada.
        context.Users.AddRange(
            NewUser(1, OrgA, "Ana", "Álvarez", "ana@reservarte.com"),
            NewUser(2, OrgA, "Beatriz", "Bravo", "bea@reservarte.com"),
            NewUser(3, OrgA, "Carmen", "Cano", "carmen@reservarte.com"),
            NewUser(4, OrgB, "Diana", "Duarte", "diana@otrocentro.com"));

        context.Employees.AddRange(
            NewEmployee(1, OrgA, "Ana", "Álvarez", "ana@reservarte.com"),
            NewEmployee(2, OrgA, "Beatriz", "Bravo", "bea@reservarte.com"),
            NewEmployee(3, OrgA, "Carmen", "Cano", "carmen@reservarte.com", isActive: false),
            NewEmployee(4, OrgB, "Diana", "Duarte", "diana@otrocentro.com"));

        context.EmployeeAvailabilities.AddRange(
            NewAvailability(1, OrgA, WeekDay.Monday, "09:00", "18:00"),
            NewAvailability(1, OrgA, WeekDay.Tuesday, "09:00", "14:00"),
            NewAvailability(4, OrgB, WeekDay.Monday, "10:00", "19:00"));

        context.EmployeeExceptions.AddRange(
            NewException(1, OrgA, new DateTime(2026, 9, 14), new DateTime(2026, 9, 18)),
            NewException(1, OrgA, new DateTime(2026, 12, 24), new DateTime(2026, 12, 26)));

        context.SaveChanges();
    }

    private static User NewUser(
        int id, Guid organizationId, string firstName, string lastName, string email) => new()
        {
            Id = id,
            OrganizationId = organizationId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Rol = "employee",
            SecurityStamp = Guid.NewGuid().ToString(),
        };

    private static Employee NewEmployee(
        int id, Guid organizationId, string firstName, string lastName, string email,
        bool isActive = true) => new()
        {
            Id = id,
            OrganizationId = organizationId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Rol = "employee",
            IsActive = isActive,
        };

    private static EmployeeAvailability NewAvailability(
        int employeeId, Guid organizationId, int dayOfWeek, string start, string end) => new()
        {
            EmployeeId = employeeId,
            OrganizationId = organizationId,
            DayOfWeek = dayOfWeek,
            StartTime = TimeOnly.Parse(start),
            EndTime = TimeOnly.Parse(end),
        };

    private static EmployeeException NewException(
        int employeeId, Guid organizationId, DateTime start, DateTime end) => new()
        {
            EmployeeId = employeeId,
            OrganizationId = organizationId,
            StartDateTime = start,
            EndDateTime = end,
            Type = EmployeeExceptionTypes.Vacation,
        };

    // ── Aislamiento multi-tenant ──────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_solo_devuelve_empleados_de_la_organizacion_actual()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new EmployeeFilter());

        result.Items.Should().OnlyContain(e => e.OrganizationId == OrgA);
        result.Items.Should().NotContain(e => e.Email == "diana@otrocentro.com");
    }

    [Fact]
    public async Task Sin_organizacion_resuelta_no_se_devuelve_ningun_empleado()
    {
        using var context = CreateContext(organizationId: null);
        var repository = CreateRepository(context, organizationId: null);

        var result = await repository.GetPagedAsync(new EmployeeFilter());

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task El_query_filter_impide_ver_disponibilidades_de_otra_organizacion()
    {
        using var context = CreateContext(OrgA);

        // Consulta DIRECTA a la tabla, sin pasar por Employee: es justo el caso
        // que antes cruzaba organizaciones (RA-869f17myx).
        var todas = await context.EmployeeAvailabilities.ToListAsync();

        todas.Should().OnlyContain(a => a.OrganizationId == OrgA);
        todas.Should().HaveCount(2);
    }

    [Fact]
    public async Task El_query_filter_impide_ver_ausencias_de_otra_organizacion()
    {
        using var contextB = CreateContext(OrgB);

        var deOrgB = await contextB.EmployeeExceptions.ToListAsync();

        // Las dos ausencias sembradas son de OrgA.
        deOrgB.Should().BeEmpty();
    }

    [Fact]
    public async Task Cada_organizacion_ve_su_propia_disponibilidad()
    {
        using var contextB = CreateContext(OrgB);

        var deOrgB = await contextB.EmployeeAvailabilities.ToListAsync();

        deOrgB.Should().ContainSingle();
        deOrgB[0].OrganizationId.Should().Be(OrgB);
    }

    // ── Filtros y paginación ──────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_por_defecto_excluye_los_empleados_dados_de_baja()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new EmployeeFilter());

        result.Items.Should().OnlyContain(e => e.IsActive);
        result.Items.Should().NotContain(e => e.FirstName == "Carmen");
    }

    [Fact]
    public async Task GetPagedAsync_puede_pedir_explicitamente_las_bajas()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new EmployeeFilter { IsActive = false });

        result.Items.Should().ContainSingle();
        result.Items[0].FirstName.Should().Be("Carmen");
    }

    [Fact]
    public async Task GetPagedAsync_busca_por_nombre_apellidos_y_email()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var porNombre = await repository.GetPagedAsync(new EmployeeFilter { Search = "Beatriz" });
        var porApellido = await repository.GetPagedAsync(new EmployeeFilter { Search = "Bravo" });
        var porEmail = await repository.GetPagedAsync(new EmployeeFilter { Search = "bea@" });

        porNombre.Items.Should().ContainSingle();
        porApellido.Items.Should().ContainSingle();
        porEmail.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetPagedAsync_cuenta_el_total_del_filtro_y_no_el_de_la_pagina()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new EmployeeFilter { Page = 1, PageSize = 1 });

        result.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(2); // Ana y Beatriz (Carmen está de baja)
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_ordena_por_apellidos_y_nombre()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new EmployeeFilter());

        result.Items.Select(e => e.LastName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetPagedAsync_acota_el_tamano_de_pagina_a_un_maximo()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new EmployeeFilter { PageSize = 5_000 });

        result.PageSize.Should().Be(100);
    }

    // ── Consultas puntuales ───────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_no_encuentra_un_empleado_de_otra_organizacion()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var propio = await repository.GetByIdAsync(1);
        var ajeno = await repository.GetByIdAsync(4);

        propio.Should().NotBeNull();
        ajeno.Should().BeNull();
    }

    [Fact]
    public async Task EmailExistsAsync_ignora_al_propio_empleado_al_editarlo()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var chocaConOtro = await repository.EmailExistsAsync("bea@reservarte.com");
        var consigoMismo = await repository.EmailExistsAsync("bea@reservarte.com", excludeEmployeeId: 2);

        chocaConOtro.Should().BeTrue();
        consigoMismo.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailabilitiesAsync_devuelve_el_horario_ordenado_por_dia_y_hora()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var horario = await repository.GetAvailabilitiesAsync(employeeId: 1);

        horario.Should().HaveCount(2);
        horario.Select(a => a.DayOfWeek).Should().ContainInOrder(WeekDay.Monday, WeekDay.Tuesday);
    }

    [Fact]
    public async Task GetExceptionsAsync_devuelve_las_ausencias_que_solapan_el_intervalo()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var deSeptiembre = await repository.GetExceptionsAsync(
            employeeId: 1, from: new DateTime(2026, 9, 1), to: new DateTime(2026, 9, 30));

        deSeptiembre.Should().ContainSingle();
        deSeptiembre[0].StartDateTime.Should().Be(new DateTime(2026, 9, 14));
    }

    [Fact]
    public async Task GetExceptionsAsync_incluye_una_ausencia_que_envuelve_el_intervalo()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        // Intervalo contenido por completo dentro de la ausencia del 14 al 18.
        var resultado = await repository.GetExceptionsAsync(
            employeeId: 1, from: new DateTime(2026, 9, 15), to: new DateTime(2026, 9, 16));

        resultado.Should().ContainSingle();
    }

    [Fact]
    public async Task GetExceptionsAsync_excluye_las_ausencias_fuera_del_intervalo()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var resultado = await repository.GetExceptionsAsync(
            employeeId: 1, from: new DateTime(2026, 10, 1), to: new DateTime(2026, 10, 31));

        resultado.Should().BeEmpty();
    }

    // ── Escritura ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ReplaceAvailabilitiesAsync_sustituye_el_horario_completo()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        await repository.ReplaceAvailabilitiesAsync(employeeId: 1, new[]
        {
            NewAvailability(1, OrgA, WeekDay.Friday, "08:00", "15:00"),
        });
        await repository.SaveChangesAsync();

        var horario = await repository.GetAvailabilitiesAsync(employeeId: 1);

        horario.Should().ContainSingle();
        horario[0].DayOfWeek.Should().Be(WeekDay.Friday);
    }

    [Fact]
    public async Task ReplaceAvailabilitiesAsync_impone_el_empleado_y_el_tenant_de_la_peticion()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        // Entrada maliciosa: apunta a otro empleado y a otra organización.
        var intruso = NewAvailability(999, OrgB, WeekDay.Wednesday, "10:00", "12:00");

        await repository.ReplaceAvailabilitiesAsync(employeeId: 1, new[] { intruso });
        await repository.SaveChangesAsync();

        intruso.EmployeeId.Should().Be(1);
        intruso.OrganizationId.Should().Be(OrgA);
    }

    [Fact]
    public async Task ReplaceAvailabilitiesAsync_no_toca_el_horario_de_otro_empleado()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        await repository.ReplaceAvailabilitiesAsync(employeeId: 1, Array.Empty<EmployeeAvailability>());
        await repository.SaveChangesAsync();

        using var contextB = CreateContext(OrgB);
        var horarioDeOtraOrg = await contextB.EmployeeAvailabilities.ToListAsync();

        horarioDeOtraOrg.Should().ContainSingle();
    }

    [Fact]
    public async Task Update_sella_la_fecha_de_modificacion()
    {
        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var empleada = await repository.GetByIdAsync(1);
        empleada!.Phone = "600123123";
        repository.Update(empleada);
        await repository.SaveChangesAsync();

        empleada.UpdatedAt.Should().NotBeNull();
        empleada.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }
}
