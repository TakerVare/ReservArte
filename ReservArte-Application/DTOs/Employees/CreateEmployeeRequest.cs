namespace ReservArte.Application.DTOs.Employees;

/// <summary>
/// Alta de empleado. No lleva contraseña a propósito: el alta crea la cuenta
/// sin credencial local y el empleado la establece él mismo con el flujo de
/// recuperación (`/auth/forgot-password`), de modo que la contraseña nunca
/// pasa por quien da el alta.
/// </summary>
public class CreateEmployeeRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }

    /// <summary>Uno de `Roles.AssignableToEmployee`. Por defecto, el de menor privilegio.</summary>
    public string Rol { get; init; } = "Employee";

    public string? ProfileImageUrl { get; init; }
    public DateOnly? HireDate { get; init; }
}
