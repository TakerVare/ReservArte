using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Options;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Implementación del emisor de tokens (vol. 2 §9.2.1) adaptada al User de
/// Identity (Id int) y a la configuración tipada JwtOptions.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    // Claim propietario del tenant. Mismo nombre que consumirá la
    // resolución de organización desde el JWT (ICurrentOrganizationService).
    public const string OrganizationIdClaimType = "organization_id";

    // Claim que marca un ticket intermedio de 2FA: el token NO autoriza
    // operaciones, solo el canje en /auth/mfa/verify.
    public const string MfaPendingClaimType = "mfa_pending";

    // Minutos de validez del ticket intermedio (ventana corta: el usuario
    // teclea el código del móvil en este margen)
    private const int MfaTicketMinutes = 5;

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(User user, Guid organizationId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(OrganizationIdClaimType, organizationId.ToString()),
            new("role", user.Rol),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateMfaTicket(User user, Guid organizationId)
    {
        // Claims mínimos: identidad + tenant + marca mfa_pending. SIN role
        // ni jti de sesión: este token no representa una sesión iniciada.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(OrganizationIdClaimType, organizationId.ToString()),
            new(MfaPendingClaimType, "true"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(MfaTicketMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Token opaco de 64 bytes aleatorios (no es un JWT): se persistirá y
        // podrá revocarse en la tarea de endpoints de autenticación.
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler
        {
            // Sin remapeo: los claims se leen con su nombre original (sub,
            // organization_id, mfa_pending...) y no traducidos a las URIs
            // largas de ClaimTypes. Coherente con MapInboundClaims=false del
            // JwtBearer; imprescindible para leer "sub" al canjear el ticket.
            MapInboundClaims = false,
        };
        var key = Encoding.UTF8.GetBytes(_options.SecretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateLifetime = true,
                // Sin margen de tolerancia: la expiración es exacta
                ClockSkew = TimeSpan.Zero,
            }, out _);

            return principal;
        }
        catch
        {
            // Token inválido (firma, emisor, audiencia o expiración): el
            // llamador decide la respuesta (401 con AUTH_* en su tarea)
            return null;
        }
    }
}