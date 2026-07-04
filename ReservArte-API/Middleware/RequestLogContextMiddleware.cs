using Serilog.Context;

namespace ReservArte.API.Middleware;

/// <summary>
/// Adjunta RequestId y OrganizationId a todos los eventos de log emitidos
/// durante la petición HTTP (vía Serilog LogContext).
/// </summary>
public class RequestLogContextMiddleware
{
    private const string OrganizationHeaderName = "X-Organization-Id";

    private readonly RequestDelegate _next;

    public RequestLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var organizationId = ResolveOrganizationId(context);

        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        using (LogContext.PushProperty(TenantMiddleware.OrganizationItemKey,
                   organizationId ?? "anonymous"))
        {
            await _next(context);
        }
    }

    private static string? ResolveOrganizationId(HttpContext context)
    {
        // 1) Si TenantMiddleware ya resolvió (no es el caso con el orden
        //    actual del pipeline, pero mantiene el contrato a prueba de
        //    reordenaciones futuras)
        if (context.Items.TryGetValue(TenantMiddleware.OrganizationItemKey,
                out var fromItems) && fromItems is not null)
        {
            return fromItems.ToString();
        }

        // 2) Cabecera directa (dev): valor aún sin validar contra BD;
        //    la versión validada la aporta TenantMiddleware aguas abajo
        var fromHeader = context.Request.Headers[OrganizationHeaderName].ToString();
        return string.IsNullOrWhiteSpace(fromHeader) ? null : fromHeader;
    }
}