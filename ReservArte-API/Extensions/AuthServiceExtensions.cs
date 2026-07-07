using ReservArte.Application.Interfaces;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Services;

namespace ReservArte.API.Extensions;

public static class AuthServiceExtensions
{
    /// <summary>
    /// Registra el binding de la sección "Jwt" (vol. 1 §5.1.3) y el emisor
    /// de tokens IJwtTokenService. La autenticación JwtBearer en el pipeline
    /// y los endpoints de login se añaden en las siguientes subtareas.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}