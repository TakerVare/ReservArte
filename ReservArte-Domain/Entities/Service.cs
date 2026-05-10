namespace ReservArte.Domain.Entities;

public class Service
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal BasePrice { get; set; }
    public int? CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresAllergyTest { get; set; }
    public int AllergyTestHoursBefore { get; set; } = 48;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ServiceCategory? Category { get; set; }
    public ICollection<ServiceVariation> Variations { get; set; } = [];
    public ICollection<ServicePricing> Pricings { get; set; } = [];
    public ICollection<EmployeeService> Employees { get; set; } = [];
    public ICollection<ServiceProduct> Products { get; set; } = [];
    public ICollection<ServicePackageItem> PackageItems { get; set; } = [];
    public ICollection<ServicePromotion> Promotions { get; set; } = [];
    public ICollection<WaitingList> WaitingLists { get; set; } = [];
}
