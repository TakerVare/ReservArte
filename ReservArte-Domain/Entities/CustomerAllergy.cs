namespace ReservArte.Domain.Entities;

/// <summary>Alergia o dato médico relevante para prestar los servicios.</summary>
public class CustomerAllergy
{
    public int Id { get; set; }

    /// <summary>
    /// Tenant propietario. Redundante con Customer.OrganizationId a propósito:
    /// permite el query filter global sin depender de un JOIN (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int CustomerId { get; set; }
    public string AllergyDescription { get; set; } = string.Empty;

    /// <summary>Valores de <see cref="AllergySeverities"/>.</summary>
    public string Severity { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}

/// <summary>Valores admitidos por <see cref="CustomerAllergy.Severity"/>.</summary>
public static class AllergySeverities
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Low,
        Medium,
        High,
    };
}
