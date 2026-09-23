using AutoMapper;
using Microsoft.Extensions.Logging;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Máquina de estados de la cita (RA-869d7f4xf, vol. 1 §5.2.2).
///
/// Cada transición sigue el mismo guion: resolver tenant, comprobar quién llama,
/// cargar la cita, comprobar que el estado de partida la admite, aplicarla y
/// guardar. Lo que cambia de una a otra son los estados de partida admitidos y
/// quién puede pedirla, así que ambas cosas se declaran en un único sitio y el
/// resto es común: si cada método repitiera el guion, las reglas se irían
/// separando con el tiempo.
///
/// No usa `IUnitOfWork`: no hay cuenta de Identity de por medio, solo la fila de
/// la cita (mismo criterio que ServicePackageService).
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _repository;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IMapper _mapper;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IAppointmentRepository repository,
        ICurrentOrganizationService currentOrganization,
        ICurrentUserService currentUser,
        TimeProvider timeProvider,
        IMapper mapper,
        ILogger<AppointmentService> logger)
    {
        _repository = repository;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<Result<AppointmentDto>> ConfirmAsync(
        int id, CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            from: new[] { AppointmentStatuses.Pending },
            to: AppointmentStatuses.Confirmed,
            allowedRoles: StaffRoles,
            cancellationToken: cancellationToken);

    public Task<Result<AppointmentDto>> StartAsync(
        int id, CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            from: new[] { AppointmentStatuses.Confirmed },
            to: AppointmentStatuses.InProgress,
            allowedRoles: StaffRoles,
            cancellationToken: cancellationToken);

    public Task<Result<AppointmentDto>> CompleteAsync(
        int id, CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            from: new[] { AppointmentStatuses.InProgress },
            to: AppointmentStatuses.Completed,
            allowedRoles: StaffRoles,
            cancellationToken: cancellationToken);

    public Task<Result<AppointmentDto>> MarkNoShowAsync(
        int id, CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            from: AppointmentStatuses.Blocking,
            to: AppointmentStatuses.NoShow,
            allowedRoles: ManagementRoles,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Cancelación. No pasa por <see cref="TransitionAsync"/> porque el estado
    /// de destino no está fijado de antemano: lo decide quién cancela.
    /// </summary>
    public async Task<Result<AppointmentDto>> CancelAsync(
        int id, CancelAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved();
        }

        var appointment = await _repository.GetByIdAsync(id, cancellationToken);
        if (appointment is null)
        {
            return NotFound(id);
        }

        // Quién cancela decide el estado, y de ahí sale CancelledByType: es la
        // forma de que las dos columnas no puedan contradecirse. El genérico
        // `cancelled` no lo escribe nadie (decisión del usuario); se sigue
        // aceptando al leer porque el CHECK lo admite y puede venir de una
        // importación.
        var canceller = ResolveCanceller(appointment);
        if (canceller is null)
        {
            // Una clienta sobre una cita ajena recibe el mismo 404 que si no
            // existiera: no se le confirma qué citas hay en el centro.
            return IsCustomer ? NotFound(id) : Forbidden();
        }

        if (!AppointmentStatuses.Blocking.Contains(appointment.Status))
        {
            return InvalidState(appointment.Status, "cancelar");
        }

        appointment.Status = canceller.Value.Status;
        appointment.CancelledByType = canceller.Value.Type;
        appointment.CancelledById = _currentUser.UserId;
        appointment.CancelledAt = _timeProvider.GetUtcNow().UtcDateTime;
        appointment.CancellationReason = string.IsNullOrWhiteSpace(request.Reason)
            ? null
            : request.Reason.Trim();

        // `UpdatedAt` no se toca aquí: lo sella el repositorio al marcar el
        // cambio, igual que en Empleados y Clientes. `CancelledAt` sí es de
        // aquí, porque es un dato de negocio —cuándo canceló la persona— y no
        // el sello técnico de la última escritura.
        _repository.Update(appointment);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cita {AppointmentId} cancelada como {Status} por la cuenta {UserId}",
            appointment.Id,
            appointment.Status,
            _currentUser.UserId);

        return Result<AppointmentDto>.Ok(_mapper.Map<AppointmentDto>(appointment));
    }

    // ── Guion común ───────────────────────────────────────────────────────

    private async Task<Result<AppointmentDto>> TransitionAsync(
        int id,
        IReadOnlyCollection<string> from,
        string to,
        IReadOnlyCollection<string> allowedRoles,
        CancellationToken cancellationToken)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved();
        }

        // El rol se comprueba ANTES de cargar la cita, al revés que en
        // EmployeeService: allí el permiso depende del dato (del rol del
        // empleado afectado) y hay que leerlo primero, mientras que aquí depende
        // solo de quién llama. Resolviéndolo antes, una clienta recibe el mismo
        // 403 exista la cita o no, y no puede usar la diferencia entre 403 y 404
        // para averiguar qué citas hay en el centro.
        if (_currentUser.Role is not { } role || !allowedRoles.Contains(role))
        {
            return Forbidden();
        }

        var appointment = await _repository.GetByIdAsync(id, cancellationToken);
        if (appointment is null)
        {
            return NotFound(id);
        }

        if (!from.Contains(appointment.Status))
        {
            return InvalidState(appointment.Status, to);
        }

        appointment.Status = to;

        // El sello de modificación lo pone el repositorio en `Update`.
        _repository.Update(appointment);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cita {AppointmentId} pasa a {Status} por la cuenta {UserId}",
            appointment.Id,
            to,
            _currentUser.UserId);

        return Result<AppointmentDto>.Ok(_mapper.Map<AppointmentDto>(appointment));
    }

    /// <summary>
    /// Estado y tipo de cancelación que corresponden a quien llama, o null si no
    /// puede cancelar esta cita. La clienta solo cancela **la suya**: su ficha
    /// comparte Id con su cuenta (RA-869d7f369), así que basta comparar.
    /// </summary>
    private (string Status, string Type)? ResolveCanceller(Appointment appointment)
    {
        var role = _currentUser.Role;

        if (role is not null && StaffRoles.Contains(role))
        {
            return (AppointmentStatuses.CancelledByBusiness, AppointmentCancelledByTypes.Business);
        }

        if (role == Roles.Customer && _currentUser.UserId == appointment.CustomerId)
        {
            return (AppointmentStatuses.CancelledByCustomer, AppointmentCancelledByTypes.Customer);
        }

        return null;
    }

    private bool IsCustomer => _currentUser.Role == Roles.Customer;

    // ── Catálogos de roles ────────────────────────────────────────────────

    /// <summary>Quien atiende la agenda: confirma, empieza, cierra y cancela.</summary>
    private static readonly IReadOnlyCollection<string> StaffRoles = new[]
    {
        Roles.Admin,
        Roles.Manager,
        Roles.Employee,
    };

    /// <summary>
    /// Marcar un no-show tiene consecuencias para la clienta (alimentará el
    /// contador de RA-869f2gtyv), así que se reserva a la dirección.
    /// </summary>
    private static readonly IReadOnlyCollection<string> ManagementRoles = new[]
    {
        Roles.Admin,
        Roles.Manager,
    };

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Result<AppointmentDto> TenantNotResolved() =>
        Result<AppointmentDto>.Fail(
            ErrorCodes.OrgTenantNotResolved,
            "No se ha podido resolver la organización de la petición.");

    /// <summary>
    /// Mismo resultado para «no existe» y «es de otra organización»:
    /// distinguirlos revelaría qué ids existen en otros centros.
    /// </summary>
    private static Result<AppointmentDto> NotFound(int id) =>
        Result<AppointmentDto>.Fail(ErrorCodes.GenNotFound, $"No existe la cita con id {id}.");

    private static Result<AppointmentDto> Forbidden() =>
        Result<AppointmentDto>.Fail(
            ErrorCodes.GenForbidden,
            "No tiene permiso para cambiar el estado de esta cita.");

    private static Result<AppointmentDto> InvalidState(string current, string attempted) =>
        Result<AppointmentDto>.Fail(
            ErrorCodes.AptInvalidState,
            $"Una cita en estado «{current}» no admite la transición a «{attempted}».");
}
