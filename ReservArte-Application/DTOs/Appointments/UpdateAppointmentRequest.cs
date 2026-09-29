namespace ReservArte.Application.DTOs.Appointments;

/// <summary>
/// Edición de una cita viva (RA-869d7f519): cambia la empleada, la fecha, la hora,
/// los servicios o las notas, y se recalculan fin, precio y duración. La clienta
/// no cambia: una cita es de su clienta. El estado tampoco: va por sus propias
/// transiciones.
/// </summary>
public class UpdateAppointmentRequest
{
    public int EmployeeId { get; init; }

    public DateOnly AppointmentDate { get; init; }

    public TimeOnly StartTime { get; init; }

    public IReadOnlyList<AppointmentItemRequest> Items { get; init; } = [];

    public string? Notes { get; init; }
}
