namespace ReservArte.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Rol { get; set; } = "client";
    public string? ProfileImageUrl { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string Category { get; set; } = "regular";
    public int LoyaltyPoints { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockedReason { get; set; }
    public string PreferredContactMethod { get; set; } = "email";
    public bool MarketingConsent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<CustomerNote> Notes { get; set; } = [];
    public ICollection<CustomerAllergy> Allergies { get; set; } = [];
    public ICollection<CustomerConsent> Consents { get; set; } = [];
    public ICollection<CustomerPaymentMethod> PaymentMethods { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<WaitingList> WaitingLists { get; set; } = [];
}
