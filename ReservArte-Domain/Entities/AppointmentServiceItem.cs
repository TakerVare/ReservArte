namespace ReservArte.Domain.Entities;

public class AppointmentServiceItem
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int ServiceId { get; set; }
    public int? ServiceVariationId { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public int Order { get; set; }

    public Appointment Appointment { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public ServiceVariation? ServiceVariation { get; set; }
}
