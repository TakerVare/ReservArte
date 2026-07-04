using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReservArte.API.Options;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Shared.Api;
using Serilog;
using Serilog.Context;

namespace ReservArte.API.Middleware;

/// <summary>
/// Resuelve la organización (tenant) de cada petición según
/// MultiTenant:ResolutionStrategy (vol. 1 §4.3.1 y §5.1.3):
/// - "Header": GUID en la cabecera configurada (dev, Postman/curl).
/// - "Subdomain": {subdominio}.{BaseDomain} → columna Organizations.Subdomain (prod).
/// Solo actúa sobre rutas que exigen tenant (prefijo /api, con exenciones):
/// el resto pasa de largo sin tocar BD — crítico para /health, que debe
/// poder responder aunque la base de datos esté caída.
/// Si resuelve: valida contra BD, deja el tenant en ICurrentOrganizationService
/// y HttpContext.Items, y enriquece los logs. Si no resuelve, responde 400
/// con ORG_TENANT_NOT_RESOLVED.
/// </summary>
public class TenantMiddleware
{
    public const string OrganizationItemKey = "OrganizationId";

    private const string StrategyHeader = "Header";
    private const string StrategySubdomain = "Subdomain";
    private const string DefaultHeaderName = "X-Organization-Id";

    // Rutas bajo /api que quedan fuera de la exigencia de tenant
    // (excepciones documentadas: el webhook Redsys llega firmado y sin
    // cabecera; su organización se resuelve en su propia tarea)
    private static readonly string[] TenantExemptApiPaths =
    {
        "/api/v1/payments/redsys/webhook"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;
    private readonly MultiTenantOptions _options;

    public TenantMiddleware(
        RequestDelegate next,
        ILogger<TenantMiddleware> logger,
        IOptions<MultiTenantOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        AppDbContext db,
        ICurrentOrganizationService currentOrganization,
        IDiagnosticContext diagnosticContext)
    {
        // Rutas sin exigencia de tenant (health, swagger, exenciones /api):
        // pasan de largo SIN intentar resolución — la resolución consulta
        // la BD y no debe acoplar estas rutas a su disponibilidad
        if (!RequiresTenant(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var (organizationId, failureReason) = await ResolveAsync(context, db);

        if (organizationId is Guid orgId)
        {
            currentOrganization.SetOrganization(orgId);
            context.Items[OrganizationItemKey] = orgId;

            // Enriquece el evento "HTTP ... responded" de UseSerilogRequestLogging
            // (se emite fuera de este scope, por eso va vía IDiagnosticContext)
            diagnosticContext.Set(OrganizationItemKey, orgId);

            // Y todos los logs emitidos aguas abajo llevan el valor resuelto,
            // también con estrategia Subdomain (donde no hay cabecera que leer)
            using (LogContext.PushProperty(OrganizationItemKey, orgId))
            {
                await _next(context);
            }

            return;
        }

        _logger.LogWarning(
            "Tenant no resuelto ({Reason}) para {Method} {Path} con estrategia {Strategy}",
            failureReason, context.Request.Method, context.Request.Path,
            _options.ResolutionStrategy);

        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var body = ApiResponse.Fail(
            ErrorCodes.OrgTenantNotResolved,
            $"No se pudo resolver la organización ({failureReason}). " +
            $"Estrategia activa: {_options.ResolutionStrategy}.",
            details: null,
            meta: ApiMeta.Create(context.TraceIdentifier));

        await context.Response.WriteAsJsonAsync(body);
    }

    private static bool RequiresTenant(PathString path)
    {
        if (!path.StartsWithSegments("/api"))
        {
            return false;
        }

        foreach (var exempt in TenantExemptApiPaths)
        {
            if (path.StartsWithSegments(exempt))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<(Guid? OrganizationId, string FailureReason)> ResolveAsync(
        HttpContext context, AppDbContext db)
    {
        if (string.Equals(_options.ResolutionStrategy, StrategyHeader,
                StringComparison.OrdinalIgnoreCase))
        {
            return await ResolveFromHeaderAsync(context, db);
        }

        if (string.Equals(_options.ResolutionStrategy, StrategySubdomain,
                StringComparison.OrdinalIgnoreCase))
        {
            return await ResolveFromSubdomainAsync(context, db);
        }

        return (null, $"estrategia '{_options.ResolutionStrategy}' no reconocida");
    }

    private async Task<(Guid?, string)> ResolveFromHeaderAsync(
        HttpContext context, AppDbContext db)
    {
        var headerName = string.IsNullOrWhiteSpace(_options.HeaderName)
            ? DefaultHeaderName
            : _options.HeaderName;

        var raw = context.Request.Headers[headerName].ToString();

        if (string.IsNullOrWhiteSpace(raw))
        {
            // Fallback opcional de desarrollo: DefaultOrganizationId (§5.1.3)
            if (Guid.TryParse(_options.DefaultOrganizationId, out var defaultId))
            {
                return await ValidateAsync(db, defaultId,
                    "DefaultOrganizationId configurado pero inexistente o inactivo");
            }

            return (null, $"cabecera {headerName} ausente");
        }

        if (!Guid.TryParse(raw, out var orgId))
        {
            return (null, $"cabecera {headerName} no contiene un GUID válido");
        }

        return await ValidateAsync(db, orgId, "organización inexistente o inactiva");
    }

    private async Task<(Guid?, string)> ResolveFromSubdomainAsync(
        HttpContext context, AppDbContext db)
    {
        var host = context.Request.Host.Host; // sin puerto

        if (string.IsNullOrWhiteSpace(_options.BaseDomain))
        {
            return (null, "MultiTenant:BaseDomain sin configurar");
        }

        var suffix = "." + _options.BaseDomain;

        if (!host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return (null, $"host '{host}' fuera del dominio base configurado");
        }

        var subdomain = host[..^suffix.Length];

        if (string.IsNullOrWhiteSpace(subdomain) ||
            string.Equals(subdomain, "www", StringComparison.OrdinalIgnoreCase))
        {
            return (null, "host sin subdominio de organización");
        }

        var organization = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Subdomain == subdomain && o.IsActive);

        return organization is null
            ? (null, $"subdominio '{subdomain}' no registrado")
            : (organization.Id, string.Empty);
    }

    private static async Task<(Guid?, string)> ValidateAsync(
        AppDbContext db, Guid orgId, string failureReason)
    {
        var exists = await db.Organizations
            .AsNoTracking()
            .AnyAsync(o => o.Id == orgId && o.IsActive);

        return exists ? (orgId, string.Empty) : (null, failureReason);
    }
}