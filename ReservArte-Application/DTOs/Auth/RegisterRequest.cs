namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Petición de registro local (POST /api/v1/auth/register).
/// La organización se resuelve por el TenantMiddleware (cabecera en dev,
/// subdominio en prod), no viaja en el cuerpo.
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }
}