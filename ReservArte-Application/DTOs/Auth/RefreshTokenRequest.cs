namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Petición de renovación de tokens (POST /api/v1/auth/refresh-token).
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}