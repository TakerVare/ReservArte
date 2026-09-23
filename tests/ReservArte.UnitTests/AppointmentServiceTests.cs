using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.Mapping;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Services;
using ReservArte.Shared.Api;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Máquina de estados de la cita (RA-869d7f4xf, vol. 1 §5.2.2): transiciones
/// permitidas, quién puede pedirlas y la coherencia entre `Status` y
/// `CancelledByType`, que es lo que esta tarea venía a imponer.
///
/// El acceso a datos va doble; el contrato real del repositorio lo cubre
/// AppointmentRepositoryTests contra SQLite.
/// </summary>
public class AppointmentServiceTests
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

    private const int AppointmentId = 55;
    private const int CustomerId = 4;
    private const int StaffId = 2;

    /// <summary>Instante fijo, para poder comprobar los sellos de fecha.</summary>
    private static readonly DateTimeOffset Now =
        new(2026, 9, 23, 10, 30, 0, TimeSpan.Zero);

    private readonly Mock<IAppointmentRepository> _repository = new();
    private readonly FakeCurrentOrganization _currentOrganization = new();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<AppointmentProfile>(), NullLoggerFactory.Instance).CreateMapper();

    public AppointmentServiceTests()
    {
        _currentOrganization.SetOrganization(OrgA);
    }

    // ── Camino feliz ──────────────────────────────────────────────────────

    [Fact]
    public async Task Confirm_pasa_de_pending_a_confirmed()
    {
        var appointment = GivenAppointment(AppointmentStatuses.Pending);

        var result = await CreateService(Roles.Employee, StaffId).ConfirmAsync(AppointmentId);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(AppointmentStatuses.Confirmed);
        appointment.Status.Should().Be(AppointmentStatuses.Confirmed);
        // El sello de modificación es del repositorio (`Update`), no del
        // servicio: aquí solo se comprueba que se le pide marcar el cambio.
        _repository.Verify(r => r.Update(appointment), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Start_pasa_de_confirmed_a_in_progress()
    {
        GivenAppointment(AppointmentStatuses.Confirmed);

        var result = await CreateService(Roles.Employee, StaffId).StartAsync(AppointmentId);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(AppointmentStatuses.InProgress);
    }

    [Fact]
    public async Task Complete_pasa_de_in_progress_a_completed()
    {
        GivenAppointment(AppointmentStatuses.InProgress);

        var result = await CreateService(Roles.Employee, StaffId).CompleteAsync(AppointmentId);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(AppointmentStatuses.Completed);
    }

    [Fact]
    public async Task MarkNoShow_lo_permite_la_direccion()
    {
        GivenAppointment(AppointmentStatuses.Confirmed);

        var result = await CreateService(Roles.Manager, StaffId).MarkNoShowAsync(AppointmentId);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(AppointmentStatuses.NoShow);
    }

    /// <summary>
    /// El no-show se admite desde los tres estados vivos: hay abandono a mitad
    /// de servicio, no solo plantón antes de empezar.
    /// </summary>
    [Theory]
    [InlineData(AppointmentStatuses.Pending)]
    [InlineData(AppointmentStatuses.Confirmed)]
    [InlineData(AppointmentStatuses.InProgress)]
    public async Task MarkNoShow_admite_cualquier_estado_vivo(string status)
    {
        GivenAppointment(status);

        var result = await CreateService(Roles.Admin, StaffId).MarkNoShowAsync(AppointmentId);

        result.Success.Should().BeTrue();
    }

    // ── Transiciones prohibidas ───────────────────────────────────────────

    /// <summary>
    /// Desde un estado terminal no se vuelve a uno abierto: reagendar es una
    /// cita nueva (vol. 1 §5.2.2).
    /// </summary>
    [Theory]
    [InlineData(AppointmentStatuses.Completed)]
    [InlineData(AppointmentStatuses.Cancelled)]
    [InlineData(AppointmentStatuses.CancelledByCustomer)]
    [InlineData(AppointmentStatuses.CancelledByBusiness)]
    [InlineData(AppointmentStatuses.NoShow)]
    public async Task Confirm_desde_un_estado_terminal_falla(string status)
    {
        GivenAppointment(status);

        var result = await CreateService(Roles.Admin, StaffId).ConfirmAsync(AppointmentId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.AptInvalidState);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(AppointmentStatuses.Completed)]
    [InlineData(AppointmentStatuses.CancelledByBusiness)]
    [InlineData(AppointmentStatuses.NoShow)]
    public async Task MarkNoShow_desde_un_estado_terminal_falla(string status)
    {
        GivenAppointment(status);

        var result = await CreateService(Roles.Admin, StaffId).MarkNoShowAsync(AppointmentId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.AptInvalidState);
    }

    /// <summary>
    /// Saltarse la confirmación no vale: el diagrama de §5.2.2 no tiene arista
    /// de `pending` a `in_progress`.
    /// </summary>
    [Fact]
    public async Task Start_desde_pending_falla()
    {
        GivenAppointment(AppointmentStatuses.Pending);

        var result = await CreateService(Roles.Employee, StaffId).StartAsync(AppointmentId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.AptInvalidState);
    }

    [Fact]
    public async Task Complete_desde_confirmed_falla()
    {
        GivenAppointment(AppointmentStatuses.Confirmed);

        var result = await CreateService(Roles.Employee, StaffId).CompleteAsync(AppointmentId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.AptInvalidState);
    }

    /// <summary>Confirmar dos veces tampoco: no es idempotente, es un error de flujo.</summary>
    [Fact]
    public async Task Confirm_de_una_cita_ya_confirmada_falla()
    {
        GivenAppointment(AppointmentStatuses.Confirmed);

        var result = await CreateService(Roles.Admin, StaffId).ConfirmAsync(AppointmentId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.AptInvalidState);
    }

    // ── Cancelación y coherencia de las dos columnas ──────────────────────

    /// <summary>
    /// Lo que esta tarea venía a imponer: el estado y `CancelledByType` no
    /// pueden contradecirse, porque los fija el servicio a la vez.
    /// </summary>
    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Manager)]
    [InlineData(Roles.Employee)]
    public async Task Cancel_por_el_personal_deja_estado_y_tipo_de_negocio(string role)
    {
        var appointment = GivenAppointment(AppointmentStatuses.Confirmed);

        var result = await CreateService(role, StaffId)
            .CancelAsync(AppointmentId, new CancelAppointmentRequest { Reason = "  La empleada enferma  " });

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(AppointmentStatuses.CancelledByBusiness);
        result.Data.CancelledByType.Should().Be(AppointmentCancelledByTypes.Business);
        result.Data.CancelledById.Should().Be(StaffId);
        result.Data.CancelledAt.Should().Be(Now.UtcDateTime);
        result.Data.CancellationReason.Should().Be("La empleada enferma");
        appointment.IsActive.Should().BeTrue("cancelar no es dar de baja la cita");
    }

    [Fact]
    public async Task Cancel_por_la_clienta_duena_deja_estado_y_tipo_de_cliente()
    {
        GivenAppointment(AppointmentStatuses.Pending);

        var result = await CreateService(Roles.Customer, CustomerId)
            .CancelAsync(AppointmentId, new CancelAppointmentRequest());

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(AppointmentStatuses.CancelledByCustomer);
        result.Data.CancelledByType.Should().Be(AppointmentCancelledByTypes.Customer);
        result.Data.CancelledById.Should().Be(CustomerId);
        result.Data.CancellationReason.Should().BeNull("el motivo es opcional para la clienta");
    }

    /// <summary>
    /// Una clienta sobre la cita de otra recibe 404, no 403: un 403 le
    /// confirmaría que esa cita existe en el centro.
    /// </summary>
    [Fact]
    public async Task Cancel_por_una_clienta_que_no_es_la_duena_es_404()
    {
        GivenAppointment(AppointmentStatuses.Confirmed);

        var result = await CreateService(Roles.Customer, CustomerId + 1)
            .CancelAsync(AppointmentId, new CancelAppointmentRequest());

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(AppointmentStatuses.Completed)]
    [InlineData(AppointmentStatuses.CancelledByCustomer)]
    [InlineData(AppointmentStatuses.NoShow)]
    public async Task Cancel_desde_un_estado_terminal_falla(string status)
    {
        GivenAppointment(status);

        var result = await CreateService(Roles.Admin, StaffId)
            .CancelAsync(AppointmentId, new CancelAppointmentRequest());

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.AptInvalidState);
    }

    /// <summary>
    /// El genérico `cancelled` no lo escribe nadie (decisión del usuario):
    /// siempre se deduce de quién cancela, que es lo que mantiene coherentes las
    /// dos columnas.
    /// </summary>
    [Fact]
    public async Task Cancel_nunca_deja_el_estado_generico()
    {
        GivenAppointment(AppointmentStatuses.Pending);

        var result = await CreateService(Roles.Admin, StaffId)
            .CancelAsync(AppointmentId, new CancelAppointmentRequest());

        result.Data!.Status.Should().NotBe(AppointmentStatuses.Cancelled);
        result.Data.CancelledByType.Should().NotBeNull();
    }

    // ── Permisos ──────────────────────────────────────────────────────────

    /// <summary>
    /// La clienta no gestiona la agenda: solo puede cancelar lo suyo, no
    /// confirmar, empezar ni cerrar.
    /// </summary>
    [Fact]
    public async Task Confirm_por_una_clienta_es_403()
    {
        GivenAppointment(AppointmentStatuses.Pending);

        var result = await CreateService(Roles.Customer, CustomerId).ConfirmAsync(AppointmentId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
    }

    /// <summary>
    /// Marcar un no-show tiene consecuencias para la clienta, así que una
    /// empleada de a pie no llega: es cosa de la dirección.
    /// </summary>
    [Fact]
    public async Task MarkNoShow_por_una_empleada_es_403()
    {
        GivenAppointment(AppointmentStatuses.Confirmed);

        var result = await CreateService(Roles.Employee, StaffId).MarkNoShowAsync(AppointmentId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
    }

    /// <summary>
    /// Sin permiso, la respuesta es la misma exista la cita o no: si el 403 se
    /// diera solo con citas existentes, la diferencia con el 404 diría qué citas
    /// hay en el centro.
    /// </summary>
    [Fact]
    public async Task Confirm_sin_permiso_no_revela_si_la_cita_existe()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        var result = await CreateService(Roles.Customer, CustomerId).ConfirmAsync(AppointmentId);

        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
        _repository.Verify(
            r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Tenant y cita inexistente ─────────────────────────────────────────

    [Fact]
    public async Task Confirm_sin_organizacion_resuelta_falla()
    {
        _currentOrganization.Clear();

        var result = await CreateService(Roles.Admin, StaffId).ConfirmAsync(AppointmentId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.OrgTenantNotResolved);
    }

    [Fact]
    public async Task Cancel_sin_organizacion_resuelta_falla()
    {
        _currentOrganization.Clear();

        var result = await CreateService(Roles.Admin, StaffId)
            .CancelAsync(AppointmentId, new CancelAppointmentRequest());

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.OrgTenantNotResolved);
    }

    [Fact]
    public async Task Confirm_de_una_cita_inexistente_es_404()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Appointment?)null);

        var result = await CreateService(Roles.Admin, StaffId).ConfirmAsync(AppointmentId);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    // ── Andamiaje ─────────────────────────────────────────────────────────

    private Appointment GivenAppointment(string status)
    {
        var appointment = new Appointment
        {
            Id = AppointmentId,
            OrganizationId = OrgA,
            CustomerId = CustomerId,
            EmployeeId = StaffId,
            AppointmentDate = new DateOnly(2026, 10, 16),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Status = status,
            TotalPrice = 25m,
            IsActive = true,
        };

        _repository
            .Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointment);

        return appointment;
    }

    private AppointmentService CreateService(string role, int userId) =>
        new(
            _repository.Object,
            _currentOrganization,
            new FakeCurrentUser { Role = role, UserId = userId },
            new FixedTimeProvider(Now),
            _mapper,
            NullLogger<AppointmentService>.Instance);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public int? UserId { get; init; }

        public string? Role { get; init; }
    }

    private sealed class FakeCurrentOrganization : ICurrentOrganizationService
    {
        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;

        public void Clear() => OrganizationId = null;
    }
}
