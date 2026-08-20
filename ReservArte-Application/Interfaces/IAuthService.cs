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
    /// <summary>
    /// Canjea el ticket intermedio de 2FA (mfa_pending) + el código (TOTP o
    /// de recuperación) por el par de tokens definitivo. Vol. 1 §4.4.2.
    /// </summary>
    Task<AuthResult<AuthResponse>> VerifyMfaAsync(
        string mfaTicket, string code, Guid organizationId, string? ipAddress);

    /// <summary>
    /// Login social tras el callback del IdP (vol. 1 §4.4.1): localiza al
    /// usuario por su vínculo en AspNetUserLogins; si no existe, vincula por
    /// email a un usuario existente de la organización; y si tampoco, crea
    /// una cuenta solo-social (sin contraseña local).
    /// </summary>
    Task<AuthResult<AuthResponse>> ExternalLoginAsync(
        string provider,
        string providerKey,
        string? email,
        string? firstName,
        string? lastName,
        Guid organizationId,
        string? ipAddress);

    /// <summary>Siempre completa sin revelar si el email existe (anti-enumeración).</summary>
    Task ForgotPasswordAsync(string email, Guid organizationId);
}