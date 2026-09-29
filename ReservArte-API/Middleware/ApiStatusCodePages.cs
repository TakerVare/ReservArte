using Microsoft.AspNetCore.Diagnostics;
using ReservArte.Shared.Api;

namespace ReservArte.API.Middleware;

/// <summary>
/// Cuerpo con envelope para las respuestas de error que el enrutado deja vacías
/// (RA-869f1k17q): 404 de una ruta que no existe (o cuyo parámetro no casa con la
/// restricción, como <c>/customers/abc</c>) y 405 de un método no admitido. Solo
/// actúa bajo <c>/api</c>: Swagger y health tienen sus propias respuestas.
/// </summary>
public static class ApiStatusCodePages
{
    public static Task WriteAsync(StatusCodeContext statusContext)
    {
        var context = statusContext.HttpContext;

        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            return Task.CompletedTask;
        }

        return context.Response.StatusCode switch
        {
            StatusCodes.Status404NotFound => ApiErrorWriter.WriteAsync(
                context, ErrorCodes.GenNotFound, "No existe el recurso solicitado."),
            StatusCodes.Status405MethodNotAllowed => ApiErrorWriter.WriteAsync(
                context, ErrorCodes.GenMethodNotAllowed, "Este recurso no admite ese método."),
            _ => Task.CompletedTask,
        };
    }
}
