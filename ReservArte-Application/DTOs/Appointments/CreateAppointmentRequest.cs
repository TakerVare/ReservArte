namespace ReservArte.Application.DTOs.Appointments;

/// <summary>
/// Alta de una cita por el personal (RA-869d7f519). La hora de fin, el precio y
/// la duración los calcula el servidor a partir de las líneas: la cita dura lo
/// que suman sus servicios. Quién la crea sale de la sesión, no del cuerpo.
/// </summary>
public class CreateAppointmentRequest
{
    public int CustomerId { get; init; }

    public int EmployeeId { get; init; }

    /// <summary>Fecha de la cita, en el calendario del centro.</summary>
    public DateOnly AppointmentDate { get; init; }

    /// <summary>Hora de inicio, en la hora local del centro.</summary>
    public TimeOnly StartTime { get; init; }

    /// <summary>Servicios en su orden de prestación; al menos uno.</summary>
    public IReadOnlyList<AppointmentItemRequest> Items { get; init; } = [];

    /// <summary>Notas internas del personal (2000 caracteres como mucho).</summary>
    public string? Notes { get; init; }
}
