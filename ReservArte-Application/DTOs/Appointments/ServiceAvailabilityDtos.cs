namespace ReservArte.Application.DTOs.Appointments;

/// <summary>
/// Ventana de reserva de quien consulta (H-44): de hoy a hoy más las semanas de su
/// rol, ambas fechas incluidas, en el calendario del centro.
/// </summary>
public class BookingWindowDto
{
    public DateOnly BookableFrom { get; init; }

    public DateOnly BookableUntil { get; init; }
}

/// <summary>Huecos libres de un empleado, dentro de la respuesta por servicio.</summary>
public class EmployeeSlotsDto
{
    public int EmployeeId { get; init; }

    public string EmployeeName { get; init; } = string.Empty;

    /// <summary>Huecos libres, ordenados por hora. Nunca vacío: sin huecos, el empleado no sale.</summary>
    public IReadOnlyList<TimeSlotDto> Slots { get; init; } = Array.Empty<TimeSlotDto>();
}

/// <summary>
/// Huecos de un servicio en una fecha, agrupados por los empleados que lo prestan
/// (H-45). Un empleado sin el servicio asignado o sin huecos ese día no aparece.
/// Fuera de la ventana de reserva, la lista va vacía.
/// </summary>
public class ServiceAvailabilityResponse : BookingWindowDto
{
    public int ServiceId { get; init; }

    public DateOnly Date { get; init; }

    /// <summary>Duración del servicio, en minutos: todos los huecos duran esto.</summary>
    public int DurationMinutes { get; init; }

    public int SlotStepMinutes { get; init; }

    public IReadOnlyList<EmployeeSlotsDto> Employees { get; init; } = Array.Empty<EmployeeSlotsDto>();
}

/// <summary>
/// Días de un intervalo en los que algún empleado puede prestar el servicio (H-45),
/// para marcar el calendario. Solo días dentro de la ventana de reserva.
/// </summary>
public class ServiceAvailableDaysResponse : BookingWindowDto
{
    public int ServiceId { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public IReadOnlyList<DateOnly> Days { get; init; } = Array.Empty<DateOnly>();
}
