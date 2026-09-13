namespace ReservArte.Application.DTOs.Employees;

/// <summary>Ficha de empleado tal como la expone la API.</summary>
public class EmployeeDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;

    /// <summary>Nombre y apellidos, para listados y selectores.</summary>
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Rol { get; init; } = string.Empty;
    public string? ProfileImageUrl { get; init; }
    public DateOnly? HireDate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // OrganizationId NO se expone: el tenant lo resuelve el servidor por
    // cabecera o subdominio, y devolverlo solo daría pistas del aislamiento.
}
