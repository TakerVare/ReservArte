namespace ReservArte.Domain.Entities;

/// <summary>Nota interna sobre un cliente, visible solo para el personal.</summary>
public class CustomerNote
{
    public int Id { get; set; }

    /// <summary>
    /// Tenant propietario. Redundante con Customer.OrganizationId a propósito:
    /// permite el query filter global sin depender de un JOIN (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int CustomerId { get; set; }

    /// <summary>Autor de la nota.</summary>
    public int EmployeeId { get; set; }

    public string Note { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
