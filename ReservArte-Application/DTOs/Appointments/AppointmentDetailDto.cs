namespace ReservArte.Application.DTOs.Appointments;

/// <summary>Ficha de la cita (RA-869d7f519): el resumen más sus líneas de servicio.</summary>
public class AppointmentDetailDto : AppointmentSummaryDto
{
    /// <summary>Líneas en su orden de prestación.</summary>
    public IReadOnlyList<AppointmentServiceItemDto> Items { get; init; } = [];

    /// <summary>
    /// Avisos que no bloquean la cita (RA-869f9cu2x): hoy, la prueba de alergia previa.
    /// Los calcula el servidor al leer la ficha y al crear o editar la cita; el
    /// historial de la clienta no los trae. Con setter porque se rellenan después
    /// de mapear.
    /// </summary>
    public IReadOnlyList<AppointmentWarningDto> Warnings { get; set; } = [];
}
