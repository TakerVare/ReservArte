using Microsoft.AspNetCore.Diagnostics;
using ReservArte.Shared.Api;

namespace ReservArte.API.Middleware;

/// <summary>
/// Última red de la API (RA-869f74u70): una excepción no controlada sale como
/// 500 <c>GEN_INTERNAL_ERROR</c> con envelope, en vez de un 500 vacío. El detalle
/// va al log, con el RequestId de la petición, que también viaja en
/// <c>meta.requestId</c> para enlazar respuesta y log. Solo en Development la
/// respuesta incluye el tipo y el mensaje de la excepción; nunca la traza.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // El cliente cortó la petición: no es un fallo del servidor ni hay a quién responder.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Petición cancelada por el cliente: {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            // 499 (convención de nginx, «el cliente cerró la petición»): así el log de la
            // petición no la cuenta como un 500.
            httpContext.Response.StatusCode = 499;
            return true;
        }

        _logger.LogError(
            exception,
            "Excepción no controlada en {Method} {Path} (RequestId {RequestId})",
            httpContext.Request.Method, httpContext.Request.Path, httpContext.TraceIdentifier);

        // Si ya salió parte de la respuesta no se puede cambiar el status ni el cuerpo:
        // se deja que el servidor corte la conexión.
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var details = _environment.IsDevelopment()
            ? new { exception = exception.GetType().FullName, message = exception.Message }
            : null;

        await ApiErrorWriter.WriteAsync(
            httpContext,
            ErrorCodes.GenInternalError,
            "Se ha producido un error inesperado. Si persiste, indica el requestId al soporte.",
            details,
            cancellationToken);

        return true;
    }
}
