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
/// Repositorio de citas (RA-869d7f4n4) sobre SQLite en memoria: el aislamiento
/// por tenant, los filtros de la agenda y el orden son SQL real, que un doble no
/// ejercita. Mismo criterio que los repositorios de Clientes y de paquetes.
/// </summary>
public class AppointmentRepositoryTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private const int Ana = 1;
    private const int Carmen = 2;
    private const int Diana = 3;
    private const int Sofia = 4;
    private const int Lucia = 5;

    private const int ServicioA = 1;
    private const int ServicioOtroCentro = 2;

    private static readonly DateOnly Lunes = new(2026, 10, 5);
    private static readonly DateOnly Martes = new(2026, 10, 6);
    private static readonly DateOnly Miercoles = new(2026, 10, 7);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AppointmentRepositoryTests()
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

    private static AppointmentRepository CreateRepository(
        AppDbContext context, Guid? organizationId) =>
        new(context, new Tenant(organizationId));

    /// <summary>
    /// Dos centros con su personal, sus clientas y su catálogo. Sin tenant, como
    /// un seeder: el query filter global no restringe y se siembra todo.
    /// </summary>
    private static void Seed(AppDbContext context)
    {
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "More Than Brows", Subdomain = "morethanbrows" },
            new Organization { Id = OrgB, Name = "Otro Centro", Subdomain = "otrocentro" });

        foreach (var (id, org, email, rol) in new[]
                 {
                     (Ana, OrgA, "ana@orga.com", Roles.Employee),
                     (Carmen, OrgA, "carmen@orga.com", Roles.Customer),
                     (Diana, OrgB, "diana@orgb.com", Roles.Employee),
                     (Sofia, OrgB, "sofia@orgb.com", Roles.Customer),
                     (Lucia, OrgA, "lucia@orga.com", Roles.Employee),
                 })
        {
            context.Users.Add(new User
            {
                Id = id,
                OrganizationId = org,
                FirstName = "N",
                LastName = "A",
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Rol = rol,
                SecurityStamp = Guid.NewGuid().ToString(),
            });
        }

        context.Employees.AddRange(
            NewEmployee(Ana, OrgA, "Ana", "ana@orga.com"),
            NewEmployee(Lucia, OrgA, "Lucía", "lucia@orga.com"),
            NewEmployee(Diana, OrgB, "Diana", "diana@orgb.com"));

        context.Customers.AddRange(
            NewCustomer(Carmen, OrgA, "Carmen", "carmen@orga.com"),
            NewCustomer(Sofia, OrgB, "Sofía", "sofia@orgb.com"));

        context.Services.AddRange(
            new Service
            {
                Id = ServicioA,
                OrganizationId = OrgA,
                Name = "Diseño de cejas",
                DurationMinutes = 45,
                BasePrice = 25m,
            },
            new Service
            {
                Id = ServicioOtroCentro,
                OrganizationId = OrgB,
                Name = "Lifting",
                DurationMinutes = 60,
                BasePrice = 40m,
            });

        context.SaveChanges();
    }

    private static Employee NewEmployee(int id, Guid org, string name, string email) => new()
    {
        Id = id,
        OrganizationId = org,
        FirstName = name,
        LastName = "Apellido",
        Email = email,
    };

    private static Customer NewCustomer(int id, Guid org, string name, string email) => new()
    {
        Id = id,
        OrganizationId = org,
        FirstName = name,
        LastName = "Apellido",
        Email = email,
    };

    private static Appointment NewAppointment(
        Guid org,
        int customerId,
        int employeeId,
        DateOnly date,
        int hour = 10,
        string status = AppointmentStatuses.Pending,
        bool isActive = true,
        string? redsysOrderNumber = null) => new()
        {
            OrganizationId = org,
            CustomerId = customerId,
            EmployeeId = employeeId,
            AppointmentDate = date,
            StartTime = new TimeOnly(hour, 0),
            EndTime = new TimeOnly(hour, 45),
            Status = status,
            TotalPrice = 25m,
            IsActive = isActive,
            RedsysOrderNumber = redsysOrderNumber,
        };

    /// <summary>Siembra citas sin tenant resuelto, como haría un seeder.</summary>
    private void SeedAppointments(params Appointment[] appointments)
    {
        using var context = CreateContext(organizationId: null);
        context.Appointments.AddRange(appointments);
        context.SaveChanges();
    }

    // ── Aislamiento por organización ──────────────────────────────────────

    [Fact]
    public async Task La_lista_solo_devuelve_citas_del_centro_actual()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes),
            NewAppointment(OrgB, Sofia, Diana, Lunes));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new AppointmentFilter());

        result.Items.Should().ContainSingle()
            .Which.OrganizationId.Should().Be(OrgA);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Sin_organizacion_resuelta_el_repositorio_no_devuelve_nada()
    {
        // El query filter global deja pasar todo sin tenant (lo necesitan
        // migraciones y seeders); el repositorio, en cambio, no debe servir la
        // agenda de nadie.
        SeedAppointments(NewAppointment(OrgA, Carmen, Ana, Lunes));

        using var context = CreateContext(organizationId: null);
        var repository = CreateRepository(context, organizationId: null);

        var result = await repository.GetPagedAsync(new AppointmentFilter());

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Una_cita_de_otro_centro_no_se_encuentra_por_id()
    {
        SeedAppointments(NewAppointment(OrgB, Sofia, Diana, Lunes));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        (await repository.GetByIdAsync(1)).Should().BeNull();
    }

    // ── Filtros de la agenda ──────────────────────────────────────────────

    [Fact]
    public async Task Sin_filtro_de_actividad_solo_salen_las_citas_activas()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes),
            NewAppointment(OrgA, Carmen, Ana, Martes, isActive: false));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new AppointmentFilter());

        result.Items.Should().ContainSingle()
            .Which.AppointmentDate.Should().Be(Lunes);
    }

    [Fact]
    public async Task Las_citas_de_baja_se_piden_explicitamente()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes),
            NewAppointment(OrgA, Carmen, Ana, Martes, isActive: false));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new AppointmentFilter { IsActive = false });

        result.Items.Should().ContainSingle()
            .Which.AppointmentDate.Should().Be(Martes);
    }

    [Fact]
    public async Task Una_cita_cancelada_sigue_siendo_activa()
    {
        // Cancelar es una transición de Status que la clienta ve; IsActive es la
        // baja lógica de gestión. No son lo mismo.
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes, status: AppointmentStatuses.CancelledByCustomer));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new AppointmentFilter());

        result.Items.Should().ContainSingle()
            .Which.Status.Should().Be(AppointmentStatuses.CancelledByCustomer);
    }

    [Fact]
    public async Task El_rango_de_fechas_incluye_los_dos_extremos()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes),
            NewAppointment(OrgA, Carmen, Ana, Martes),
            NewAppointment(OrgA, Carmen, Ana, Miercoles));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(
            new AppointmentFilter { From = Lunes, To = Martes });

        result.Items.Select(a => a.AppointmentDate)
            .Should().BeEquivalentTo(new[] { Lunes, Martes });
    }

    [Fact]
    public async Task Se_puede_filtrar_por_empleada_y_por_clienta()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes),
            NewAppointment(OrgA, Carmen, Lucia, Lunes, hour: 12));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var deAna = await repository.GetPagedAsync(new AppointmentFilter { EmployeeId = Ana });
        var deCarmen = await repository.GetPagedAsync(new AppointmentFilter { CustomerId = Carmen });

        deAna.Items.Should().ContainSingle().Which.EmployeeId.Should().Be(Ana);
        deCarmen.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Se_puede_filtrar_por_estado()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes),
            NewAppointment(OrgA, Carmen, Ana, Martes, status: AppointmentStatuses.Confirmed));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(
            new AppointmentFilter { Status = AppointmentStatuses.Confirmed });

        result.Items.Should().ContainSingle()
            .Which.AppointmentDate.Should().Be(Martes);
    }

    // ── Orden y paginación ────────────────────────────────────────────────

    [Fact]
    public async Task La_lista_va_de_la_cita_mas_reciente_a_la_mas_antigua()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes),
            NewAppointment(OrgA, Carmen, Ana, Miercoles),
            NewAppointment(OrgA, Carmen, Ana, Martes));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new AppointmentFilter());

        result.Items.Select(a => a.AppointmentDate)
            .Should().Equal(Miercoles, Martes, Lunes);
    }

    [Fact]
    public async Task El_total_es_el_del_filtro_y_no_el_de_la_pagina()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes),
            NewAppointment(OrgA, Carmen, Ana, Martes),
            NewAppointment(OrgA, Carmen, Ana, Miercoles));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(
            new AppointmentFilter { Page = 2, PageSize = 2 });

        result.TotalCount.Should().Be(3);
        result.Items.Should().ContainSingle();
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task El_tamano_de_pagina_se_acota_a_100()
    {
        SeedAppointments(NewAppointment(OrgA, Carmen, Ana, Lunes));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var result = await repository.GetPagedAsync(new AppointmentFilter { PageSize = 500 });

        result.PageSize.Should().Be(100);
    }

    // ── Carga explícita del detalle ───────────────────────────────────────

    [Fact]
    public async Task El_detalle_trae_lineas_ordenadas_con_su_servicio_clienta_y_empleada()
    {
        var cita = NewAppointment(OrgA, Carmen, Ana, Lunes);
        cita.ServiceItems.Add(new AppointmentServiceItem
        {
            OrganizationId = OrgA,
            ServiceId = ServicioA,
            Price = 25m,
            DurationMinutes = 45,
            Order = 2,
        });
        cita.ServiceItems.Add(new AppointmentServiceItem
        {
            OrganizationId = OrgA,
            ServiceId = ServicioA,
            Price = 18m,
            DurationMinutes = 30,
            Order = 1,
        });
        SeedAppointments(cita);

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var detalle = await repository.GetDetailAsync(cita.Id);

        detalle.Should().NotBeNull();
        detalle!.ServiceItems.Select(i => i.Order).Should().Equal(1, 2);
        detalle.ServiceItems.First().Service.Name.Should().Be("Diseño de cejas");
        detalle.Customer.FirstName.Should().Be("Carmen");
        detalle.Employee.FirstName.Should().Be("Ana");
    }

    // ── Redsys ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Una_cita_se_encuentra_por_su_numero_de_pedido()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes, redsysOrderNumber: "ORD-1"));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        (await repository.GetByRedsysOrderAsync("ORD-1"))!.CustomerId.Should().Be(Carmen);
    }

    [Fact]
    public async Task El_numero_de_pedido_de_otro_centro_no_se_encuentra()
    {
        // El índice único es por columna, no por organización: sin el filtro de
        // tenant, el callback de la pasarela de un centro tocaría la cita de otro.
        SeedAppointments(
            NewAppointment(OrgB, Sofia, Diana, Lunes, redsysOrderNumber: "ORD-2"));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        (await repository.GetByRedsysOrderAsync("ORD-2")).Should().BeNull();
    }

    // ── Rango de fechas (agenda y solapes) ────────────────────────────────

    [Fact]
    public async Task El_rango_devuelve_las_citas_ordenadas_por_fecha_y_hora()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Martes, hour: 9),
            NewAppointment(OrgA, Carmen, Ana, Lunes, hour: 16),
            NewAppointment(OrgA, Carmen, Ana, Lunes, hour: 9));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var agenda = await repository.GetByDateRangeAsync(Lunes, Martes);

        agenda.Select(a => (a.AppointmentDate, a.StartTime.Hour))
            .Should().Equal((Lunes, 9), (Lunes, 16), (Martes, 9));
    }

    [Fact]
    public async Task El_rango_puede_acotarse_a_una_empleada()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes),
            NewAppointment(OrgA, Carmen, Lucia, Lunes, hour: 12));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var agenda = await repository.GetByDateRangeAsync(Lunes, Lunes, employeeId: Lucia);

        agenda.Should().ContainSingle().Which.EmployeeId.Should().Be(Lucia);
    }

    [Fact]
    public async Task El_rango_no_filtra_por_estado_pero_si_excluye_las_de_baja()
    {
        // Quien detecte solapes (RA-869d7f4rd) decidirá si una cancelada ocupa
        // hueco; el repositorio no lo decide por él. Una cita dada de baja, en
        // cambio, ya no está en la agenda.
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes, status: AppointmentStatuses.Cancelled),
            NewAppointment(OrgA, Carmen, Ana, Lunes, hour: 12, isActive: false));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var agenda = await repository.GetByDateRangeAsync(Lunes, Lunes);

        agenda.Should().ContainSingle()
            .Which.Status.Should().Be(AppointmentStatuses.Cancelled);
    }

    [Fact]
    public async Task El_rango_no_se_sale_del_centro_actual()
    {
        SeedAppointments(
            NewAppointment(OrgA, Carmen, Ana, Lunes),
            NewAppointment(OrgB, Sofia, Diana, Lunes));

        using var context = CreateContext(OrgB);
        var repository = CreateRepository(context, OrgB);

        var agenda = await repository.GetByDateRangeAsync(Lunes, Lunes);

        agenda.Should().ContainSingle().Which.OrganizationId.Should().Be(OrgB);
    }

    // ── Escritura ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_y_SaveChanges_persisten_la_cita_con_sus_lineas()
    {
        using (var context = CreateContext(OrgA))
        {
            var repository = CreateRepository(context, OrgA);
            var cita = NewAppointment(OrgA, Carmen, Ana, Lunes);
            cita.ServiceItems.Add(new AppointmentServiceItem
            {
                OrganizationId = OrgA,
                ServiceId = ServicioA,
                Price = 25m,
                DurationMinutes = 45,
                Order = 1,
            });

            repository.Add(cita);
            await repository.SaveChangesAsync();
        }

        using var check = CreateContext(OrgA);
        var guardada = await CreateRepository(check, OrgA).GetDetailAsync(1);

        guardada.Should().NotBeNull();
        guardada!.ServiceItems.Should().ContainSingle();
    }

    [Fact]
    public async Task Update_sella_la_fecha_de_modificacion()
    {
        SeedAppointments(NewAppointment(OrgA, Carmen, Ana, Lunes));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var cita = await repository.GetByIdAsync(1);
        cita!.Status = AppointmentStatuses.Confirmed;
        repository.Update(cita);
        await repository.SaveChangesAsync();

        using var check = CreateContext(OrgA);
        var guardada = await CreateRepository(check, OrgA).GetByIdAsync(1);

        guardada!.Status.Should().Be(AppointmentStatuses.Confirmed);
        guardada.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task La_cita_leida_por_id_viene_con_seguimiento_y_se_puede_modificar()
    {
        // GetByIdAsync es la lectura para escribir: si viniera AsNoTracking, un
        // SaveChanges posterior no guardaría nada y el fallo sería silencioso.
        SeedAppointments(NewAppointment(OrgA, Carmen, Ana, Lunes));

        using var context = CreateContext(OrgA);
        var repository = CreateRepository(context, OrgA);

        var cita = await repository.GetByIdAsync(1);
        cita!.Notes = "Llega 10 minutos tarde";
        await repository.SaveChangesAsync();

        using var check = CreateContext(OrgA);
        (await CreateRepository(check, OrgA).GetByIdAsync(1))!.Notes
            .Should().Be("Llega 10 minutos tarde");
    }
}
