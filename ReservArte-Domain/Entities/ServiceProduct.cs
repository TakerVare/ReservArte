namespace ReservArte.Domain.Entities;

public class ServiceProduct
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int ProductId { get; set; }
    public decimal? QuantityUsed { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Service Service { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
