namespace ReservArte.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Rol { get; set; } = "employee";
    public string? ProfileImageUrl { get; set; }
    public DateOnly? HireDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;

    // TODO Sprint 2: reincorporar Availabilities, Appointments, Services, etc.
    // cuando esas entidades entren en el DbContext
}