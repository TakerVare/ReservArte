namespace ReservArte.Domain.Entities;

public class CustomerAllergy
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string AllergyDescription { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
}
