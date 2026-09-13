namespace ReservArte.Application.DTOs.Employees;

/// <summary>
/// Edición de empleado. El email se puede cambiar, pero arrastra la cuenta de
/// acceso: el servicio lo propaga al usuario de Identity para que login y
/// ficha no queden desincronizados.
/// </summary>
public class UpdateEmployeeRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Rol { get; init; } = "Employee";
    public string? ProfileImageUrl { get; init; }
    public DateOnly? HireDate { get; init; }
}
