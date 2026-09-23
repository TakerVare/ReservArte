namespace ReservArte.Application.DTOs.Appointments;

/// <summary>
/// Hueco libre en la agenda de un empleado: el rango que ocuparía una cita de
/// la duración pedida. Es un intervalo semiabierto <c>[StartTime, EndTime)</c>,
/// así que dos huecos contiguos (11:00-12:00 y 12:00-13:00) no se pisan.
/// </summary>
public class TimeSlotDto
{
    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }
}

/// <summary>
/// Huecos libres de un empleado en una fecha, para una duración concreta
/// (RA-869d7f4rd). Se calculan al vuelo restando al horario semanal las
/// ausencias y las citas que ocupan agenda; no se guarda nada.
///
/// La petición se devuelve dentro de la respuesta (empleado, fecha y duración)
/// porque el frontend pinta varias consultas a la vez —una por empleada— y
/// necesita saber a cuál corresponde cada lista sin llevar la cuenta.
/// </summary>
public class AvailabilityResponse
{
    public int EmployeeId { get; init; }

    public DateOnly Date { get; init; }

    /// <summary>Duración pedida, en minutos: todos los huecos duran esto.</summary>
    public int DurationMinutes { get; init; }

    /// <summary>
    /// Paso de la rejilla con la que se generan los huecos, en minutos. Va en la
    /// respuesta para que el frontend no tenga que asumirlo: si algún día el
    /// paso se hace configurable por centro, la SPA ya lo está leyendo.
    /// </summary>
    public int SlotStepMinutes { get; init; }

    /// <summary>Huecos libres, ordenados por hora de inicio. Puede venir vacío.</summary>
    public IReadOnlyList<TimeSlotDto> Slots { get; init; } = Array.Empty<TimeSlotDto>();
}
