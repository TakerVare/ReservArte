using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Disponibilidad de la agenda (RA-869d7f4rd): qué huecos tiene libres un
/// empleado y si un rango concreto se puede ocupar. Queda acotado al tenant de
/// la petición, como el resto de servicios.
///
/// La disponibilidad real sale de restar al horario semanal recurrente
/// (<c>EmployeeAvailabilities</c>) las ausencias (<c>EmployeeExceptions</c>) y
/// las citas que ocupan agenda. No se guarda: se calcula en cada consulta,
/// porque cualquiera de las tres piezas cambia de un minuto a otro.
///
/// Es la pieza que usarán los endpoints de alta y reagendado de citas
/// (RA-869d7f519) antes de escribir nada.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Huecos libres de un empleado en una fecha para una cita de
    /// <paramref name="durationMinutes"/>.
    ///
    /// Un día sin horario, cubierto entero por una ausencia o ya pasado
    /// devuelve la lista vacía, no un error: no tener huecos es una respuesta
    /// legítima. Fallan, en cambio, la duración fuera de rango
    /// (GEN_VALIDATION_FAILED) y el empleado inexistente o de baja
    /// (GEN_NOT_FOUND).
    /// </summary>
    Task<Result<AvailabilityResponse>> GetAvailableSlotsAsync(
        int employeeId,
        DateOnly date,
        int durationMinutes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Comprueba que el rango <c>[startTime, endTime)</c> se puede ocupar: cae
    /// dentro del horario del empleado ese día, no pisa ninguna ausencia y no
    /// pisa ninguna cita que ocupe agenda. Si algo lo impide devuelve
    /// APT_SLOT_UNAVAILABLE, con el motivo en el mensaje.
    ///
    /// <paramref name="excludeAppointmentId"/> deja fuera una cita al
    /// comprobar: es lo que permite reagendar una cita sin que choque consigo
    /// misma.
    ///
    /// A diferencia de <see cref="GetAvailableSlotsAsync"/>, **no mira el
    /// reloj**: el personal registra a veces una cita que acaba de ocurrir, y
    /// decidir si el pasado se admite es de la máquina de estados
    /// (RA-869d7f4xf) y de los endpoints (RA-869d7f519), no de aquí.
    /// </summary>
    Task<Result<bool>> EnsureSlotAvailableAsync(
        int employeeId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);
}
