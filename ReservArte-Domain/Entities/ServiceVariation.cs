namespace ReservArte.Domain.Entities;

public class ServiceVariation
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceModifier { get; set; }
    public int DurationModifier { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Service Service { get; set; } = null!;
    public ICollection<AppointmentServiceItem> AppointmentItems { get; set; } = [];
}
