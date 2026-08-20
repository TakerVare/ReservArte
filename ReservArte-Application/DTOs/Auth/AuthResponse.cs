namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Respuesta de un login/registro. En el caso normal trae el par de tokens
/// y el usuario. Si la cuenta tiene 2FA activo, en su lugar viaja
/// MfaRequired = true + MfaTicket (canjeable en /auth/mfa/verify); los
/// campos de token quedan vacíos hasta superar el segundo factor.
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public UserDto? User { get; set; }

    public bool MfaRequired { get; set; }

    public string? MfaTicket { get; set; }
}

/// <summary>Datos públicos del usuario autenticado (nunca incluye hash ni secretos).</summary>
public class UserDto
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;
}