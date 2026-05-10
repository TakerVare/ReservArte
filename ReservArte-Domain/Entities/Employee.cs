namespace ReservArte.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
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

    public User User { get; set; } = null!;
    public ICollection<EmployeeAvailability> Availabilities { get; set; } = [];
    public ICollection<EmployeeException> Exceptions { get; set; } = [];
    public ICollection<EmployeeService> Services { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<CustomerNote> Notes { get; set; } = [];
}
