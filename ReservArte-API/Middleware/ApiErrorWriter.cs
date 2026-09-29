using ReservArte.Shared.Api;

namespace ReservArte.API.Middleware;

/// <summary>
/// Respuesta de error con envelope para lo que responde fuera de un controlador:
/// middleware de tenant, límite de peticiones, eventos de JwtBearer y manejador
/// global de excepciones (RA-869f74u70). El status sale del mapa único
/// <see cref="ErrorStatusCodes"/>, el mismo que usa <c>ApiControllerBase</c>.
/// </summary>
public static class ApiErrorWriter
{
    public static Task WriteAsync(
        HttpContext context,
        string errorCode,
        string message,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = ErrorStatusCodes.For(errorCode);

        return context.Response.WriteAsJsonAsync(
            ApiResponse.Fail(errorCode, message, details, ApiMeta.Create(context.TraceIdentifier)),
            cancellationToken);
    }
}
