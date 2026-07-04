namespace ReservArte.Domain.Entities;

public class WaitingList
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int CustomerId { get; set; }
    public int ServiceId { get; set; }
    public int? PreferredEmployeeId { get; set; }
    public DateTime? PreferredDate { get; set; }
    public DateTime DateRangeStart { get; set; }
    public DateTime DateRangeEnd { get; set; }
    public int Priority { get; set; } = 1000;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public Employee? PreferredEmployee { get; set; }
}
