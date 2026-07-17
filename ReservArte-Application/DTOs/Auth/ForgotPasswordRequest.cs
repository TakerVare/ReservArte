namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Petición de recuperación de contraseña (POST /api/v1/auth/forgot-password).
/// </summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}