namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Petición de login local (vol. 1 §4.4.1: POST /api/v1/auth/login).
/// El campo Captcha lo adjunta el frontend tras varios intentos fallidos;
/// el backend lo verifica en AuthService.LoginAsync (RA-869d7ezkp). En dev
/// (Captcha:Enabled = false) la verificación se omite.
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? Captcha { get; set; }
}