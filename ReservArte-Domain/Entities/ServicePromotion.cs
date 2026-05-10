namespace ReservArte.Domain.Entities;

public class ServicePromotion
{
    public int Id { get; set; }
    public int? ServiceId { get; set; }
    public int? ServicePackageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsSeasonalService { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Service? Service { get; set; }
    public ServicePackage? ServicePackage { get; set; }
}
