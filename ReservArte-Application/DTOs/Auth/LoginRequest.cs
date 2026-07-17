namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Petición de login local (vol. 1 §4.4.1: POST /api/v1/auth/login).
/// El campo Captcha es opcional; su verificación se añade en la tarea
/// de rate limiting + CAPTCHA (RA-869d7ezkp).
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? Captcha { get; set; }
}