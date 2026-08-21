using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ReservArte.Shared.Api;

namespace ReservArte.API.Extensions;

public static class RateLimitingServiceExtensions
{
    // Políticas nombradas que los endpoints referencian con [EnableRateLimiting]
    public const string LoginPolicy = "auth-login";
    public const string MfaVerifyPolicy = "auth-mfa-verify";

    /// <summary>
    /// Rate limiting nativo de .NET 8 (vol. 1 §4.4.3): protege los endpoints
    /// de autenticación contra fuerza bruta. Ventana fija de 1 hora,
    /// particionada por IP de cliente (el límite es por origen, no global):
    /// 10/h en login, 20/h en mfa/verify. El rechazo devuelve 429 con el
    /// envelope y error.code GEN_RATE_LIMITED.
    /// El vol. 2 §9.3.1 documenta un enfoque alternativo con la librería
    /// AspNetCoreRateLimit; aquí se usa el middleware del framework.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(LoginPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                    }));

            options.AddPolicy(MfaVerifyPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromHours(1),
                    }));

            // Respuesta unificada al superar el límite: 429 + envelope
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                // Cabecera Retry-After si el limitador informa de la espera
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                var body = ApiResponse.Fail(
                    ErrorCodes.GenRateLimited,
                    "Has superado el número de intentos permitidos. Inténtalo de nuevo más tarde.",
                    details: null,
                    meta: ApiMeta.Create(context.HttpContext.TraceIdentifier));

                await context.HttpContext.Response.WriteAsJsonAsync(body, cancellationToken);
            };
        });

        return services;
    }

    // IP de cliente como clave de partición. En producción tras proxy/CDN,
    // la IP real llega en cabeceras (X-Forwarded-For); su tratamiento se
    // afinará con la configuración de proxy de las tareas de infra.
    private static string GetClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}