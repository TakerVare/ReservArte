using System.Security.Claims;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Emisor único de tokens de la API (vol. 1 §4.4.1): genera el access JWT
/// con los claims sub/email/organization_id/role/jti, el refresh token
/// opaco, y valida access tokens entrantes.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Genera el access JWT firmado para el usuario y su organización.</summary>
    string GenerateAccessToken(User user, Guid organizationId);

    /// <summary>Genera un refresh token opaco criptográficamente aleatorio (se persiste en la tarea de endpoints).</summary>
    string GenerateRefreshToken();

    /// <summary>Valida firma, emisor, audiencia y expiración. Devuelve el principal o null si el token es inválido.</summary>
    ClaimsPrincipal? ValidateToken(string token);
}