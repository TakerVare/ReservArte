using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Mapeo de Citas, líneas de cita y lista de espera (RA-869d7f4j8): índices que
/// pide el diseño, CHECK de catálogos y de coherencia, comportamiento de las FK
/// y aislamiento por organización. Contra SQLite real, no contra dobles: lo que
/// se comprueba aquí es justo lo que hace la base de datos, no el código.
/// </summary>
public class AppointmentMappingTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private static readonly DateOnly Dia = new(2026, 10, 5);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AppointmentMappingTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
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

    private AppDbContext ContextFor(Guid? organizationId) =>
        new(_options, new Tenant(organizationId));

    /// <summary>
    /// Dos centros completos: cuenta, ficha de empleada, ficha de clienta y un
    /// servicio en cada uno. Sin tenant, como un seeder.
    /// </summary>
    private static void Seed(AppDbContext context)
    {
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "Org A", Subdomain = "org-a" },
            new Organization { Id = OrgB, Name = "Org B", Subdomain = "org-b" });

        foreach (var (id, org, email, rol) in new[]
                 {
                     (1, OrgA, "ana@orga.com", Roles.Employee),
                     (2, OrgA, "carmen@orga.com", Roles.Customer),
                     (3, OrgB, "diana@orgb.com", Roles.Employee),
                     (4, OrgB, "sofia@orgb.com", Roles.Customer),
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
            new Employee { Id = 1, OrganizationId = OrgA, FirstName = "Ana", LastName = "A", Email = "ana@orga.com" },
            new Employee { Id = 3, OrganizationId = OrgB, FirstName = "Diana", LastName = "D", Email = "diana@orgb.com" });

        context.Customers.AddRange(
            new Customer { Id = 2, OrganizationId = OrgA, FirstName = "Carmen", LastName = "L", Email = "carmen@orga.com" },
            new Customer { Id = 4, OrganizationId = OrgB, FirstName = "Sofía", LastName = "R", Email = "sofia@orgb.com" });

        context.Services.AddRange(
            new Service { Id = 1, OrganizationId = OrgA, Name = "Diseño de cejas", DurationMinutes = 45, BasePrice = 25m },
            new Service { Id = 2, OrganizationId = OrgB, Name = "Lifting", DurationMinutes = 60, BasePrice = 40m });

        context.SaveChanges();
    }

    private static Appointment NewAppointment(
        Guid organizationId,
        int customerId,
        int employeeId,
        string? redsysOrderNumber = null,
        string status = AppointmentStatuses.Pending) => new()
        {
            OrganizationId = organizationId,
            CustomerId = customerId,
            EmployeeId = employeeId,
            AppointmentDate = Dia,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 45),
            Status = status,
            TotalPrice = 25m,
            RedsysOrderNumber = redsysOrderNumber,
        };

    /// <summary>Cita de la clienta Carmen con la empleada Ana, en OrgA.</summary>
    private static Appointment CitaDeOrgA(string? redsysOrderNumber = null) =>
        NewAppointment(OrgA, customerId: 2, employeeId: 1, redsysOrderNumber);

    private static WaitingList NewWaitingEntry(Guid organizationId, int customerId, int serviceId, int priority = 1000) => new()
    {
        OrganizationId = organizationId,
        CustomerId = customerId,
        ServiceId = serviceId,
        DateRangeStart = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        DateRangeEnd = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc),
        Priority = priority,
    };

    private static IEntityType EntityType<T>(AppDbContext context) =>
        context.Model.FindEntityType(typeof(T))!;

    // ── Los índices que pide el diseño (vol. 1 §5.2) ──────────────────────

    [Fact]
    public void La_agenda_se_indexa_por_organizacion_y_fecha_con_el_nombre_del_diseno()
    {
        using var context = new AppDbContext(_options);

        var indice = EntityType<Appointment>(context).GetIndexes()
            .Single(i => i.GetDatabaseName() == "idx_appointments_org_date");

        indice.Properties.Select(p => p.Name).Should().Equal("OrganizationId", "AppointmentDate");
        indice.IsUnique.Should().BeFalse();
    }

    [Fact]
    public void El_numero_de_pedido_de_Redsys_es_unico_pero_solo_cuando_lo_hay()
    {
        using var context = new AppDbContext(_options);

        var indice = EntityType<Appointment>(context).GetIndexes()
            .Single(i => i.GetDatabaseName() == "idx_appointments_redsys_order");

        indice.IsUnique.Should().BeTrue();

        // Sin el filtro, SQL Server solo admitiría UNA cita sin número de pedido:
        // su índice único trata todos los NULL como el mismo valor.
        indice.GetFilter().Should().Be("[RedsysOrderNumber] IS NOT NULL");
    }

    [Fact]
    public void La_lista_de_espera_se_indexa_por_organizacion_servicio_y_prioridad()
    {
        using var context = new AppDbContext(_options);

        EntityType<WaitingList>(context).GetIndexes()
            .Single(i => i.GetDatabaseName() == "idx_waiting_lists_org_service_priority")
            .Properties.Select(p => p.Name)
            .Should().Equal("OrganizationId", "ServiceId", "Priority");
    }

    [Fact]
    public void Las_tablas_del_modulo_van_en_plural()
    {
        // La lista de espera nació en singular, como el ERD de diseño, y se
        // renombró para no ser la única excepción del esquema.
        using var context = new AppDbContext(_options);

        EntityType<Appointment>(context).GetTableName().Should().Be("Appointments");
        EntityType<AppointmentServiceItem>(context).GetTableName().Should().Be("AppointmentServiceItems");
        EntityType<WaitingList>(context).GetTableName().Should().Be("WaitingLists");
    }

    [Fact]
    public async Task Varias_citas_sin_numero_de_pedido_conviven()
    {
        using var context = ContextFor(OrgA);

        context.Appointments.AddRange(CitaDeOrgA(), CitaDeOrgA());

        await context.SaveChangesAsync();

        (await context.Appointments.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Dos_citas_no_pueden_repetir_el_numero_de_pedido()
    {
        using var context = ContextFor(OrgA);
        context.Appointments.AddRange(CitaDeOrgA("REDSYS-1"), CitaDeOrgA("REDSYS-1"));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // ── CHECK: el esquema no admite lo que el dominio no admite ───────────

    [Fact]
    public async Task Un_estado_fuera_del_catalogo_lo_rechaza_la_base()
    {
        using var context = ContextFor(OrgA);
        context.Appointments.Add(NewAppointment(OrgA, 2, 1, status: "reprogramada"));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "el CHECK se genera desde AppointmentStatuses");
    }

    [Theory]
    [InlineData(AppointmentStatuses.Cancelled)]
    [InlineData(AppointmentStatuses.CancelledByCustomer)]
    [InlineData(AppointmentStatuses.CancelledByBusiness)]
    [InlineData(AppointmentStatuses.NoShow)]
    public async Task Los_ocho_estados_del_diseno_los_admite_la_base(string estado)
    {
        using var context = ContextFor(OrgA);
        context.Appointments.Add(NewAppointment(OrgA, 2, 1, status: estado));

        await context.SaveChangesAsync();

        (await context.Appointments.SingleAsync()).Status.Should().Be(estado);
    }

    [Fact]
    public async Task Un_tipo_de_cancelacion_fuera_del_catalogo_lo_rechaza_la_base()
    {
        using var context = ContextFor(OrgA);
        var cita = CitaDeOrgA();
        cita.CancelledByType = "empleada";
        context.Appointments.Add(cita);

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Una_cita_sin_cancelar_no_choca_con_el_CHECK_del_tipo()
    {
        // La columna es nullable y el CHECK solo rechaza lo que evalúa a FALSE:
        // el caso normal (sin cancelar) tiene que pasar.
        using var context = ContextFor(OrgA);
        context.Appointments.Add(CitaDeOrgA());

        await context.SaveChangesAsync();

        (await context.Appointments.SingleAsync()).CancelledByType.Should().BeNull();
    }

    [Fact]
    public async Task Una_cita_que_acaba_antes_de_empezar_la_rechaza_la_base()
    {
        using var context = ContextFor(OrgA);
        var cita = CitaDeOrgA();
        cita.EndTime = new TimeOnly(9, 0);
        context.Appointments.Add(cita);

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Un_importe_negativo_lo_rechaza_la_base()
    {
        using var context = ContextFor(OrgA);
        var cita = CitaDeOrgA();
        cita.TotalPrice = -1m;
        context.Appointments.Add(cita);

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Un_rango_invertido_en_la_lista_de_espera_lo_rechaza_la_base()
    {
        using var context = ContextFor(OrgA);
        var entrada = NewWaitingEntry(OrgA, customerId: 2, serviceId: 1);
        entrada.DateRangeEnd = entrada.DateRangeStart.AddDays(-1);
        context.WaitingLists.Add(entrada);

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // ── Aislamiento por organización ──────────────────────────────────────

    [Fact]
    public async Task Las_citas_de_otra_organizacion_no_se_ven()
    {
        using (var seed = ContextFor(organizationId: null))
        {
            seed.Appointments.Add(NewAppointment(OrgA, customerId: 2, employeeId: 1));
            seed.Appointments.Add(NewAppointment(OrgB, customerId: 4, employeeId: 3));
            await seed.SaveChangesAsync();
        }

        using var context = ContextFor(OrgA);

        (await context.Appointments.ToListAsync()).Should().ContainSingle()
            .Which.OrganizationId.Should().Be(OrgA);
    }

    [Fact]
    public async Task Las_lineas_de_cita_tambien_filtran_por_su_propio_tenant()
    {
        using (var seed = ContextFor(organizationId: null))
        {
            var deA = NewAppointment(OrgA, customerId: 2, employeeId: 1);
            deA.ServiceItems.Add(new AppointmentServiceItem
            {
                OrganizationId = OrgA, ServiceId = 1, Price = 25m, DurationMinutes = 45, Order = 1,
            });

            var deB = NewAppointment(OrgB, customerId: 4, employeeId: 3);
            deB.ServiceItems.Add(new AppointmentServiceItem
            {
                OrganizationId = OrgB, ServiceId = 2, Price = 40m, DurationMinutes = 60, Order = 1,
            });

            seed.Appointments.AddRange(deA, deB);
            await seed.SaveChangesAsync();
        }

        using var context = ContextFor(OrgB);

        // La consulta va directa a las líneas, sin pasar por Appointments: sin
        // OrganizationId propio devolvería también las del otro centro.
        (await context.AppointmentServiceItems.ToListAsync()).Should().ContainSingle()
            .Which.ServiceId.Should().Be(2);
    }

    [Fact]
    public async Task La_lista_de_espera_de_otra_organizacion_no_se_ve()
    {
        using (var seed = ContextFor(organizationId: null))
        {
            seed.WaitingLists.Add(NewWaitingEntry(OrgA, customerId: 2, serviceId: 1));
            seed.WaitingLists.Add(NewWaitingEntry(OrgB, customerId: 4, serviceId: 2));
            await seed.SaveChangesAsync();
        }

        using var context = ContextFor(OrgA);

        (await context.WaitingLists.ToListAsync()).Should().ContainSingle()
            .Which.ServiceId.Should().Be(1);
    }

    // ── Qué se lleva por delante un borrado ───────────────────────────────

    [Fact]
    public void Una_cita_no_se_va_con_la_ficha_de_la_clienta_ni_con_la_de_la_empleada()
    {
        using var context = new AppDbContext(_options);
        var cita = EntityType<Appointment>(context);

        // Histórico de negocio: la baja del producto es lógica y SQL Server, de
        // hecho, rechaza los dos caminos en cascada desde AspNetUsers.
        cita.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(Customer)
                      || fk.PrincipalEntityType.ClrType == typeof(Employee))
            .Should().HaveCount(2)
            .And.OnlyContain(fk => fk.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public async Task Las_lineas_se_borran_con_su_cita()
    {
        using (var alta = ContextFor(OrgA))
        {
            var cita = CitaDeOrgA();
            cita.ServiceItems.Add(new AppointmentServiceItem
            {
                OrganizationId = OrgA, ServiceId = 1, Price = 25m, DurationMinutes = 45, Order = 1,
            });
            alta.Appointments.Add(cita);
            await alta.SaveChangesAsync();
        }

        using var baja = ContextFor(OrgA);
        baja.Appointments.Remove(await baja.Appointments.SingleAsync());
        await baja.SaveChangesAsync();

        (await baja.AppointmentServiceItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void Un_servicio_del_catalogo_no_puede_borrarse_de_debajo_de_una_cita()
    {
        using var context = new AppDbContext(_options);

        // Es el motivo por el que la baja de servicio es lógica: las citas
        // cerradas siguen apuntando a él.
        EntityType<AppointmentServiceItem>(context).GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Service))
            .DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }
}
