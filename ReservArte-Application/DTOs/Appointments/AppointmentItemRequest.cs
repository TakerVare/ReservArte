namespace ReservArte.Application.DTOs.Appointments;

/// <summary>
/// Servicio que se pide en una cita, con su variación si la hay. Precio y
/// duración no viajan: los calcula el servidor desde el catálogo.
/// </summary>
public class AppointmentItemRequest
{
    public int ServiceId { get; init; }

    public int? ServiceVariationId { get; init; }
}
