namespace ReservArte.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int? AppointmentId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string PaymentMethodType { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string? RedsysOrderNumber { get; set; }
    public string? RedsysAuthCode { get; set; }
    public string? RedsysResponse { get; set; }
    public string? RedsysTransactionType { get; set; }
    public string? RedsysCardNumber { get; set; }
    public int? CustomerPaymentMethodId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? Metadata { get; set; }
    public string? Notes { get; set; }
    public int? RegisteredById { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Appointment? Appointment { get; set; }
    public Customer Customer { get; set; } = null!;
    public CustomerPaymentMethod? CustomerPaymentMethod { get; set; }
}
