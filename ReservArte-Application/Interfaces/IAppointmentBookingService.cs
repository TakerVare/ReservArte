using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Domain.Common;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Agenda y reserva de citas (RA-869d7f519): consultar, dar de alta, editar y
/// retirar. Las transiciones de estado son de <see cref="IAppointmentService"/>.
///
/// Quién puede qué (decisiones del usuario, 2026-09-29):
/// - Leer: el personal, todas las citas del centro; la clienta, solo las suyas
///   (una ajena le da 404, no 403, para no confirmarle que existe).
/// - Crear y editar: el personal (Admin, Manager, Employee), para cualquier
///   clienta y a cualquier fecha, también pasada. La clienta (H-44), solo para sí
///   misma, con una sola cita activa y dentro de su ventana de reserva
///   (<c>CustomerBookingWindowWeeks</c>); edita solo las suyas.
/// - Retirar (baja lógica): Admin o Manager, para corregir altas erróneas. No es
///   cancelar: la cancelación es una transición que la clienta ve.
/// </summary>
public interface IAppointmentBookingService
{
    Task<Result<PagedResult<AppointmentSummaryDto>>> GetPagedAsync(
        AppointmentFilter filter, CancellationToken cancellationToken = default);

    Task<Result<AppointmentDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alta en estado <c>pending</c>. Fin, precio y duración salen del catálogo:
    /// cada línea cuesta el precio base del servicio más el de su variación y dura
    /// lo mismo más el ajuste de la variación. Guarda quién la creó.
    /// </summary>
    Task<Result<AppointmentDetailDto>> CreateAsync(
        CreateAppointmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edición de una cita que aún no ha empezado (<c>pending</c> o
    /// <c>confirmed</c>): sustituye sus líneas y recalcula fin, precio y duración.
    /// Comprueba el hueco sin chocar consigo misma.
    /// </summary>
    Task<Result<AppointmentDetailDto>> UpdateAsync(
        int id, UpdateAppointmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Historial de citas de una clienta (RA-869f2gn91), para la ficha de gestión:
    /// solo el personal, como el resto de <c>/customers</c> (la clienta ve las suyas en
    /// la agenda). Citas activas en cualquier estado, de la más reciente a la más
    /// antigua, con sus líneas. Clienta inexistente o de otro centro → 404; una dada de
    /// baja conserva su historial.
    /// </summary>
    Task<Result<PagedResult<AppointmentDetailDto>>> GetCustomerHistoryAsync(
        int customerId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Baja lógica; idempotente.</summary>
    Task<Result<AppointmentDto>> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
