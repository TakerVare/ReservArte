namespace ReservArte.Domain.Entities;

public class ServicePackage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ServicePackageItem> Items { get; set; } = [];
    public ICollection<ServicePromotion> Promotions { get; set; } = [];
}
