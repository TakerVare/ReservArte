namespace ReservArte.Application.DTOs.Appointments;

/// <summary>
/// Cita de la agenda (RA-869d7f519): los datos de <see cref="AppointmentDto"/> más
/// los nombres de la clienta y de la empleada, para que la lista no necesite una
/// consulta por fila.
/// </summary>
public class AppointmentSummaryDto : AppointmentDto
{
    public string CustomerName { get; init; } = string.Empty;

    public string EmployeeName { get; init; } = string.Empty;
}
