namespace ReservArte.Domain.Entities;

public class Appointment
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int CustomerId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string Status { get; set; } = "pending";
    public decimal TotalPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public string? RedsysOrderNumber { get; set; }
    public string? RedsysPreAuthToken { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int? CancelledById { get; set; }
    public string? CancelledByType { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public CustomerPaymentMethod? PaymentMethod { get; set; }
    public ICollection<AppointmentServiceItem> ServiceItems { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<ServicePhoto> Photos { get; set; } = [];
    public ICollection<ReminderLog> ReminderLogs { get; set; } = [];
    public ICollection<ConfirmationToken> ConfirmationTokens { get; set; } = [];
}
