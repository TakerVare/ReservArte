namespace ReservArte.Domain.Entities;

public class CustomerConsent
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public DateTime? GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
}
