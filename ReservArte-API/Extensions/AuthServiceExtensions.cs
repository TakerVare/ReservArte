using FluentValidation;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Validators.Auth;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Services;

namespace ReservArte.API.Extensions;

public static class AuthServiceExtensions
{
    /// <summary>
    /// Registra el binding de la sección "Jwt" (vol. 1 §5.1.3), el emisor
    /// de tokens, el servicio de flujos de autenticación y los validadores
    /// FluentValidation del ensamblado Application. La autenticación
    /// JwtBearer del pipeline se registra en la Fase 5 de la tarea.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Descubre y registra todos los validadores de Application
        // (LoginRequestValidator, RegisterRequestValidator, ...)
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        return services;
    }
}