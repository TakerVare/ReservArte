namespace ReservArte.API.Extensions;

public static class CorsServiceExtensions
{
    public const string DefaultPolicy = "DefaultCorsPolicy";

    /// <summary>
    /// Política de CORS para la SPA (vol. 1 §5.1): orígenes permitidos desde
    /// la sección "Cors:AllowedOrigins" (ya usada para validar el returnUrl
    /// de OAuth en ExternalAuthController, pero nunca conectada a un
    /// middleware de CORS real — sin esto, el navegador bloquea toda
    /// petición cross-origin del SPA a la API).
    /// </summary>
    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultPolicy, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        return services;
    }
}
