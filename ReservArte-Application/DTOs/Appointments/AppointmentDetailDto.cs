namespace ReservArte.Application.DTOs.Appointments;

/// <summary>Ficha de la cita (RA-869d7f519): el resumen más sus líneas de servicio.</summary>
public class AppointmentDetailDto : AppointmentSummaryDto
{
    /// <summary>Líneas en su orden de prestación.</summary>
    public IReadOnlyList<AppointmentServiceItemDto> Items { get; init; } = [];
}
