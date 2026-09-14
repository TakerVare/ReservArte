namespace ReservArte.Application.DTOs.Employees;

/// <summary>
/// Horario semanal completo de un empleado (PUT .../availability). Es un
/// reemplazo, no un parche: se manda la semana entera y sustituye a la
/// anterior, de modo que no quedan estados intermedios incoherentes mientras
/// se edita. Una lista vacía deja al empleado sin horario.
/// </summary>
public class UpdateAvailabilityRequest
{
    public IReadOnlyList<AvailabilitySlotRequest> WeeklySchedule { get; init; } =
        Array.Empty<AvailabilitySlotRequest>();
}

/// <summary>
/// Tramo del horario semanal. Ni el empleado ni la organización viajan en el
/// payload: los impone el servidor (ver `ReplaceAvailabilitiesAsync`).
/// </summary>
public class AvailabilitySlotRequest
{
    /// <summary>Convención del proyecto: 0 = lunes … 6 = domingo (helper `WeekDay`).</summary>
    public int DayOfWeek { get; init; }

    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }

    public bool IsRecurring { get; init; } = true;
}
