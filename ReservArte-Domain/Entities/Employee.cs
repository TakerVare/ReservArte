namespace ReservArte.Domain.Entities;

public class Employee
{
    /// <summary>
    /// Clave primaria compartida con <see cref="User"/>: el esquema no tiene
    /// columna UserId, el Id del empleado ES el del usuario (ver
    /// EmployeeConfiguration y la FK Employees.Id → Users.Id).
    /// </summary>
    public int Id { get; set; }

    public Guid OrganizationId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Rol { get; set; } = Roles.Employee;
    public string? ProfileImageUrl { get; set; }
    public DateOnly? HireDate { get; set; }

    /// <summary>Baja lógica: los empleados se desactivan, nunca se borran.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;

    /// <summary>Horario semanal recurrente del empleado.</summary>
    public ICollection<EmployeeAvailability> Availabilities { get; set; } =
        new List<EmployeeAvailability>();

    /// <summary>
    /// Ausencias puntuales (vacaciones, baja, formación…) que rompen el
    /// horario recurrente para un intervalo concreto.
    /// </summary>
    public ICollection<EmployeeException> Exceptions { get; set; } =
        new List<EmployeeException>();

    /// <summary>
    /// Servicios que está capacitada para prestar, con su nivel de destreza en
    /// cada uno.
    /// </summary>
    public ICollection<EmployeeServiceAssignment> Services { get; set; } =
        new List<EmployeeServiceAssignment>();

    // Appointments llega con su propio módulo (citas), no antes: hoy esa
    // entidad no está en el DbContext.
}
