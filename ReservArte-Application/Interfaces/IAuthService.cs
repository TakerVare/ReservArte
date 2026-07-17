using ReservArte.Application.DTOs.Auth;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Flujos de autenticación local (vol. 1 §4.4.1): login, registro,
/// renovación con rotación de refresh token y recuperación de contraseña.
/// La organización llega resuelta por el TenantMiddleware.
/// </summary>
public interface IAuthService
{
    Task<AuthResult<AuthResponse>> LoginAsync(LoginRequest request, Guid organizationId, string? ipAddress);

    Task<AuthResult<AuthResponse>> RegisterAsync(RegisterRequest request, Guid organizationId, string? ipAddress);

    Task<AuthResult<AuthResponse>> RefreshTokenAsync(string refreshToken, string? ipAddress);

    /// <summary>Siempre completa sin revelar si el email existe (anti-enumeración).</summary>
    Task ForgotPasswordAsync(string email, Guid organizationId);
}