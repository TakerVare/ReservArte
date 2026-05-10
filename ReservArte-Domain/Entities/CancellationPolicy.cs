namespace ReservArte.Domain.Entities;

public class CancellationPolicy
{
    public int Id { get; set; }
    public int MinHoursBeforeCancel { get; set; } = 24;
    public int PenaltyPercentage { get; set; }
    public int MaxNoShowsBeforeBlock { get; set; } = 3;
    public int? VipMinHoursBeforeCancel { get; set; }
    public int? VipPenaltyPercentage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
