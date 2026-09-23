using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Ciclo de vida de una cita (RA-869d7f4xf, vol. 1 §5.2.2): las transiciones
/// de estado y nada más. El alta, la edición y la agenda son de RA-869d7f519.
///
/// Reglas comunes a todas las transiciones:
/// <list type="bullet">
/// <item>Desde un estado terminal (<c>completed</c>, las tres cancelaciones y
/// <c>no_show</c>) no se vuelve a un estado abierto: <b>409 APT_INVALID_STATE</b>.
/// Reagendar es una cita nueva.</item>
/// <item>Una cita de otra organización o inexistente da el mismo
/// <b>404 GEN_NOT_FOUND</b>: distinguirlas revelaría qué ids existen en otros
/// centros.</item>
/// <item>Ninguna transición toca <c>IsActive</c>. Cancelar no es dar de baja:
/// la baja retira la cita de las listas de gestión y la cancelación es una fase
/// del ciclo de vida que la clienta ve.</item>
/// </list>
/// </summary>
public interface IAppointmentService
{
    /// <summary>
    /// <c>pending → confirmed</c>, por confirmación manual del personal o por
    /// pago correcto. Solo personal (Admin, Manager o Employee).
    /// </summary>
    Task<Result<AppointmentDto>> ConfirmAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>confirmed → in_progress</c>, cuando la clienta llega y empieza el
    /// servicio. Solo personal. Desde <c>pending</c> **no** se puede: hay que
    /// confirmar antes, tal como manda el diagrama de §5.2.2.
    /// </summary>
    Task<Result<AppointmentDto>> StartAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>in_progress → completed</c>, al cerrar el servicio. Solo personal.
    /// Es el estado que alimentará la promoción de categoría de la clienta
    /// (RA-869f2g02q).
    /// </summary>
    Task<Result<AppointmentDto>> CompleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela una cita viva (<c>pending</c>, <c>confirmed</c> o
    /// <c>in_progress</c>) y **fija el estado según quién cancela**: el personal
    /// deja <c>cancelled_by_business</c> y la clienta dueña de la cita,
    /// <c>cancelled_by_customer</c>; <c>CancelledByType</c> se rellena a juego.
    /// Así el servicio impone la coherencia entre las dos columnas, que es lo
    /// que esta tarea venía a resolver.
    ///
    /// Una clienta sobre una cita que no es suya recibe <b>404</b>, no 403: no
    /// se le confirma que exista.
    ///
    /// Registra motivo, fecha y cuenta que cancela. **No aplica penalización
    /// económica**: eso necesita `OrganizationSettings` y la pasarela, y vive en
    /// RA-869f6ae9h.
    /// </summary>
    Task<Result<AppointmentDto>> CancelAsync(
        int id, CancelAppointmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca la cita como no presentada, desde cualquier estado vivo. **Solo
    /// Admin o Manager**: tiene consecuencias para la clienta (alimentará el
    /// contador de no-shows de RA-869f2gtyv), así que no la deja a cualquiera.
    /// </summary>
    Task<Result<AppointmentDto>> MarkNoShowAsync(int id, CancellationToken cancellationToken = default);
}
