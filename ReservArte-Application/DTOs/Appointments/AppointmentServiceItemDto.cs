namespace ReservArte.Application.DTOs.Appointments;

/// <summary>
/// Línea de servicio de una cita. Precio y duración son los que quedaron fijados
/// al reservar, no los del catálogo de hoy: si el catálogo cambia, la cita no.
/// </summary>
public class AppointmentServiceItemDto
{
    public int ServiceId { get; init; }

    public string ServiceName { get; init; } = string.Empty;

    public int? ServiceVariationId { get; init; }

    public string? ServiceVariationName { get; init; }

    public decimal Price { get; init; }

    public int DurationMinutes { get; init; }

    /// <summary>Posición en la cita, empezando en 1.</summary>
    public int Order { get; init; }
}
