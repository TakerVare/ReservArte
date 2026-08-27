namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Petición de restablecimiento de contraseña (POST /api/v1/auth/reset-password).
/// El token llega por el enlace del email; el email lo introduce el usuario en
/// el formulario (no viaja en la URL).
/// </summary>
public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}