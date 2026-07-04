namespace ReservArte.Domain.Entities;

public class CustomerPaymentMethod
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string RedsysToken { get; set; } = string.Empty;
    public string? RedsysCofTxnid { get; set; }
    public string CardLast4 { get; set; } = string.Empty;
    public string CardBrand { get; set; } = string.Empty;
    public string CardExpiry { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
