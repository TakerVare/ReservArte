using Serilog.Context;

namespace ReservArte.API.Middleware;

/// <summary>
/// Adjunta RequestId y OrganizationId a todos los eventos de log emitidos
/// durante la petición HTTP (vía Serilog LogContext).
/// </summary>
public class RequestLogContextMiddleware
{
    private const string OrganizationHeaderName = "X-Organization-Id";
    private const string OrganizationItemKey = "OrganizationId";

    private readonly RequestDelegate _next;

    public RequestLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var organizationId = ResolveOrganizationId(context);

        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        using (LogContext.PushProperty("OrganizationId", organizationId ?? "anonymous"))
        {
            await _next(context);
        }
    }

    private static string? ResolveOrganizationId(HttpContext context)
    {
        // 1) Cuando exista TenantMiddleware (tarea 869d7eymj), dejará el
        //    OrganizationId ya resuelto en HttpContext.Items: fuente prioritaria.
        if (context.Items.TryGetValue(OrganizationItemKey, out var fromItems) &&
            fromItems is not null)
        {
            return fromItems.ToString();
        }

        // 2) Mientras tanto: en dev el tenant se resuelve por cabecera
        //    (MultiTenant:ResolutionStrategy = Header, volumen 1 §5.1.3).
        var fromHeader = context.Request.Headers[OrganizationHeaderName].ToString();
        return string.IsNullOrWhiteSpace(fromHeader) ? null : fromHeader;
    }
}