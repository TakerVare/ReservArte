using Microsoft.OpenApi;

namespace ReservArte.API.Extensions;

public static class SwaggerServiceExtensions
{
    /// <summary>
    /// Configura Swagger/OpenAPI documentando el contrato de respuesta:
    /// envelope { success, data, error, meta } (volumen 1 §5.1.1) y
    /// catálogo error.code (§5.1.2). Los tipos ApiResponse&lt;T&gt;, ApiError,
    /// ApiMeta y ApiPagination generan esquemas reutilizables en
    /// components/schemas al ser referenciados por los endpoints.
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ReservArte API",
                Version = "v1",
                Description =
                    "API multi-tenant de gestión de reservas para centros de belleza y bienestar. " +
                    "Todas las respuestas JSON usan el envelope { success, data, error, meta }: " +
                    "con success=false el cliente debe ramificar por error.code (catálogo estable " +
                    "MAYUSCULAS_SNAKE_CASE, p. ej. GEN_VALIDATION_FAILED, ORG_TENANT_NOT_RESOLVED, " +
                    "PAY_REDSYS_DECLINED), nunca por el texto de error.message. meta.requestId " +
                    "permite correlacionar la respuesta con los logs del servidor. " +
                    "Excepciones sin envelope: webhooks Redsys y health checks."
            });
        });

        return services;
    }
}