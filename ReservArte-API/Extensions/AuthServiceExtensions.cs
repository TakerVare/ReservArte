using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Validators.Auth;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Services;

namespace ReservArte.API.Extensions;

public static class AuthServiceExtensions
{
    /// <summary>
    /// Registra el binding de la sección "Jwt" (vol. 1 §5.1.3), el emisor
    /// de tokens, el servicio de flujos de autenticación, los validadores
    /// FluentValidation y el esquema de autenticación JwtBearer que protege
    /// los endpoints [Authorize]. Los parámetros de validación replican los
    /// de JwtTokenService.ValidateToken (misma clave, issuer, audience y
    /// ClockSkew cero), y RoleClaimType = "role" para que [Authorize(Roles)]
    /// lea el claim corto que emitimos.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var secretKey = jwtSection["SecretKey"] ?? string.Empty;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Sin remapeo de claims de Microsoft: las claves entrantes se
                // leen tal cual las emite JwtTokenService (sub, email, role,
                // organization_id), no se traducen a las URIs largas de
                // ClaimTypes. Imprescindible para leer "sub" por su nombre.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    // [Authorize(Roles=...)] leerá el claim corto "role".
                    // NameClaimType se deja por defecto: no designamos "sub"
                    // como claim de nombre para poder leerlo por su clave.
                    RoleClaimType = "role",
                };
            });

        return services;
    }
}