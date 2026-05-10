namespace ReservArte.Domain.Entities;

public class ServicePricing
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string EmployeeLevel { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Service Service { get; set; } = null!;
}
