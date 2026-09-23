using AutoMapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.Mapping;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Persistence.Repositories;
using ReservArte.Infrastructure.Services;
using ReservArte.Shared.Api;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Máquina de estados (RA-869d7f4xf) sobre el repositorio **real** y SQLite en
/// memoria, no sobre un doble. Aquí se comprueban las dos cosas que un mock no
/// puede demostrar: que la transición **queda escrita** en la base, y que el
/// aislamiento por tenant tapa las citas de otro centro aunque existan.
///
/// Esta tarea no tiene endpoints (son de RA-869d7f519), así que este es su
/// ejercicio funcional, el mismo criterio que RA-869d7f4n4.
/// </summary>
public class AppointmentStateMachineIntegrationTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private const int AnaEmpleadaA = 1;
    private const int CarmenClientaA = 2;
    private const int DianaEmpleadaB = 3;
    private const int SofiaClientaB = 4;

    private const int CitaDelCentroA = 10;
    private const int CitaDelCentroB = 20;

    private static readonly DateTimeOffset Now = new(2026, 9, 23, 10, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Viernes = new(2026, 10, 16);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AppointmentStateMachineIntegrationTests()
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

    /// <summary>
    /// La transición no se queda en memoria: se relee en un contexto nuevo, que
    /// es la única forma de saber que se guardó de verdad.
    /// </summary>
    [Fact]
    public async Task Confirmar_persiste_el_estado_en_la_base()
    {
        using (var context = CreateContext(OrgA))
        {
            var service = CreateService(context, OrgA, Roles.Employee, AnaEmpleadaA);

            var result = await service.ConfirmAsync(CitaDelCentroA);

            result.Success.Should().BeTrue();
        }

        using var verification = CreateContext(OrgA);
        var stored = await verification.Appointments.SingleAsync(a => a.Id == CitaDelCentroA);

        stored.Status.Should().Be(AppointmentStatuses.Confirmed);
        // Lo sella el repositorio con el reloj real, así que solo se comprueba
        // que quedó marcado; el instante exacto lo fija `CancelledAt`, que sí
        // sale del reloj inyectado.
        stored.UpdatedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Las dos columnas quedan escritas a juego, que es lo que esta tarea venía
    /// a imponer.
    /// </summary>
    [Fact]
    public async Task Cancelar_persiste_estado_y_tipo_coherentes()
    {
        using (var context = CreateContext(OrgA))
        {
            var service = CreateService(context, OrgA, Roles.Customer, CarmenClientaA);

            var result = await service.CancelAsync(
                CitaDelCentroA, new CancelAppointmentRequest { Reason = "No puedo ir" });

            result.Success.Should().BeTrue();
        }

        using var verification = CreateContext(OrgA);
        var stored = await verification.Appointments.SingleAsync(a => a.Id == CitaDelCentroA);

        stored.Status.Should().Be(AppointmentStatuses.CancelledByCustomer);
        stored.CancelledByType.Should().Be(AppointmentCancelledByTypes.Customer);
        stored.CancelledById.Should().Be(CarmenClientaA);
        stored.CancelledAt.Should().Be(Now.UtcDateTime);
        stored.CancellationReason.Should().Be("No puedo ir");
        stored.IsActive.Should().BeTrue("cancelar no es dar de baja la cita");
    }

    /// <summary>
    /// Aislamiento real: la cita del otro centro **existe** en la base, y aun
    /// así el centro A recibe 404. Un doble nunca habría enseñado esto.
    /// </summary>
    [Fact]
    public async Task Una_cita_de_otro_centro_es_404_aunque_exista()
    {
        using var context = CreateContext(OrgA);
        var service = CreateService(context, OrgA, Roles.Admin, AnaEmpleadaA);

        var result = await service.ConfirmAsync(CitaDelCentroB);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);

        using var verification = CreateContext(OrgB);
        var stored = await verification.Appointments.SingleAsync(a => a.Id == CitaDelCentroB);
        stored.Status.Should().Be(AppointmentStatuses.Pending, "no se tocó la cita del otro centro");
    }

    /// <summary>
    /// Una transición rechazada no debe dejar rastro: ni el estado ni el sello
    /// de modificación cambian.
    /// </summary>
    [Fact]
    public async Task Una_transicion_prohibida_no_escribe_nada()
    {
        using (var seed = CreateContext(OrgA))
        {
            var appointment = await seed.Appointments.SingleAsync(a => a.Id == CitaDelCentroA);
            appointment.Status = AppointmentStatuses.Completed;
            await seed.SaveChangesAsync();
        }

        using (var context = CreateContext(OrgA))
        {
            var service = CreateService(context, OrgA, Roles.Admin, AnaEmpleadaA);

            var result = await service.ConfirmAsync(CitaDelCentroA);

            result.ErrorCode.Should().Be(ErrorCodes.AptInvalidState);
        }

        using var verification = CreateContext(OrgA);
        var stored = await verification.Appointments.SingleAsync(a => a.Id == CitaDelCentroA);

        stored.Status.Should().Be(AppointmentStatuses.Completed);
        stored.UpdatedAt.Should().BeNull();
    }

    /// <summary>
    /// La clienta del otro centro tampoco puede cancelar por el camino de
    /// cliente, aunque su id coincidiera: la cita ni siquiera es visible.
    /// </summary>
    [Fact]
    public async Task Una_clienta_de_otro_centro_no_puede_cancelar()
    {
        using var context = CreateContext(OrgA);
        var service = CreateService(context, OrgA, Roles.Customer, SofiaClientaB);

        var result = await service.CancelAsync(CitaDelCentroB, new CancelAppointmentRequest());

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    // ── Andamiaje ─────────────────────────────────────────────────────────

    private sealed class Tenant(Guid? organizationId) : ICurrentOrganizationService
    {
        public Guid? OrganizationId { get; private set; } = organizationId;

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid id) => OrganizationId = id;
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public int? UserId { get; init; }

        public string? Role { get; init; }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private AppDbContext CreateContext(Guid? organizationId) =>
        new(_options, new Tenant(organizationId));

    private static AppointmentService CreateService(
        AppDbContext context, Guid organizationId, string role, int userId)
    {
        var tenant = new Tenant(organizationId);
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<AppointmentProfile>(), NullLoggerFactory.Instance).CreateMapper();

        return new AppointmentService(
            new AppointmentRepository(context, tenant),
            tenant,
            new FakeCurrentUser { Role = role, UserId = userId },
            new FixedTimeProvider(Now),
            mapper,
            NullLogger<AppointmentService>.Instance);
    }

    /// <summary>
    /// Dos centros, con una cita pendiente cada uno. Sin tenant, como un seeder:
    /// el query filter global no restringe y se siembra todo.
    /// </summary>
    private static void Seed(AppDbContext context)
    {
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "More Than Brows", Subdomain = "morethanbrows" },
            new Organization { Id = OrgB, Name = "Otro Centro", Subdomain = "otrocentro" });

        foreach (var (id, org, email, rol) in new[]
                 {
                     (AnaEmpleadaA, OrgA, "ana@orga.com", Roles.Employee),
                     (CarmenClientaA, OrgA, "carmen@orga.com", Roles.Customer),
                     (DianaEmpleadaB, OrgB, "diana@orgb.com", Roles.Employee),
                     (SofiaClientaB, OrgB, "sofia@orgb.com", Roles.Customer),
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
            NewEmployee(AnaEmpleadaA, OrgA, "Ana", "ana@orga.com"),
            NewEmployee(DianaEmpleadaB, OrgB, "Diana", "diana@orgb.com"));

        context.Customers.AddRange(
            NewCustomer(CarmenClientaA, OrgA, "Carmen", "carmen@orga.com"),
            NewCustomer(SofiaClientaB, OrgB, "Sofía", "sofia@orgb.com"));

        context.Appointments.AddRange(
            NewAppointment(CitaDelCentroA, OrgA, CarmenClientaA, AnaEmpleadaA),
            NewAppointment(CitaDelCentroB, OrgB, SofiaClientaB, DianaEmpleadaB));

        context.SaveChanges();
    }

    private static Employee NewEmployee(int id, Guid org, string name, string email) =>
        new()
        {
            Id = id,
            OrganizationId = org,
            FirstName = name,
            LastName = "Apellido",
            Email = email,
            Rol = Roles.Employee,
            HireDate = new DateOnly(2026, 1, 1),
        };

    private static Customer NewCustomer(int id, Guid org, string name, string email) =>
        new()
        {
            Id = id,
            OrganizationId = org,
            FirstName = name,
            LastName = "Apellido",
            Email = email,
            Category = CustomerCategories.New,
            PreferredContactMethod = CustomerContactMethods.Email,
        };

    private static Appointment NewAppointment(int id, Guid org, int customerId, int employeeId) =>
        new()
        {
            Id = id,
            OrganizationId = org,
            CustomerId = customerId,
            EmployeeId = employeeId,
            AppointmentDate = Viernes,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Status = AppointmentStatuses.Pending,
            TotalPrice = 25m,
        };
}
