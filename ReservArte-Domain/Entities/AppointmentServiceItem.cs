namespace ReservArte.Domain.Entities;

/// <summary>
/// Servicio prestado dentro de una cita, con su posición en la secuencia.
///
/// El precio y la duración se **copian** del servicio (y de su variación) al
/// crear la cita, no se leen del catálogo al consultarla: si el centro sube la
/// tarifa después, una cita ya cerrada debe seguir valiendo lo que se cobró.
/// </summary>
public class AppointmentServiceItem
{
    public int Id { get; set; }

    /// <summary>
    /// Tenant propietario. Redundante con Appointment.OrganizationId a
    /// propósito: permite el query filter global sin depender de un JOIN
    /// (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int AppointmentId { get; set; }
    public int ServiceId { get; set; }

    /// <summary>Variante elegida, si la hay.</summary>
    public int? ServiceVariationId { get; set; }

    /// <summary>Precio congelado en el momento de la reserva.</summary>
    public decimal Price { get; set; }

    /// <summary>Duración congelada en el momento de la reserva.</summary>
    public int DurationMinutes { get; set; }

    /// <summary>Orden de prestación dentro de la cita.</summary>
    public int Order { get; set; }

    public Appointment Appointment { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public ServiceVariation? ServiceVariation { get; set; }
    public Organization Organization { get; set; } = null!;
}
