namespace ReservArte.Shared.Api;

/// <summary>
/// Catálogo de códigos de error de aplicación error.code (volumen 1 §5.1.2).
/// Prefijo por dominio, MAYUSCULAS_SNAKE_CASE. Lista extensible: los códigos
/// nuevos se añaden aquí y en OpenAPI antes de usarse en producción.
/// </summary>
public static class ErrorCodes
{
    // ── Genéricos ─────────────────────────────────────────────────────────
    /// <summary>HTTP 500 — Error no esperado; no filtrar detalles internos en producción.</summary>
    public const string GenInternalError = "GEN_INTERNAL_ERROR";

    /// <summary>HTTP 404 — Recurso inexistente o no visible para el tenant/usuario.</summary>
    public const string GenNotFound = "GEN_NOT_FOUND";

    /// <summary>HTTP 401 — Sin autenticación o token inválido/expirado.</summary>
    public const string GenUnauthorized = "GEN_UNAUTHORIZED";

    /// <summary>HTTP 403 — Autenticado pero sin permiso o política.</summary>
    public const string GenForbidden = "GEN_FORBIDDEN";

    /// <summary>HTTP 409 — Conflicto genérico (versión, duplicado) si no aplica uno más específico.</summary>
    public const string GenConflict = "GEN_CONFLICT";

    /// <summary>HTTP 400 — Entrada inválida; usar error.details por campo.</summary>
    public const string GenValidationFailed = "GEN_VALIDATION_FAILED";

    /// <summary>HTTP 429 — Límite de peticiones excedido.</summary>
    public const string GenRateLimited = "GEN_RATE_LIMITED";

    // ── Autenticación ─────────────────────────────────────────────────────
    /// <summary>HTTP 401 — Login rechazado (credenciales incorrectas).</summary>
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";

    /// <summary>HTTP 401 — Refresh token inválido o revocado.</summary>
    public const string AuthRefreshInvalid = "AUTH_REFRESH_INVALID";

    /// <summary>HTTP 400 — Código TOTP o de recuperación incorrecto.</summary>
    public const string AuthMfaInvalid = "AUTH_MFA_INVALID";

    // ── Organización / multi-tenant ───────────────────────────────────────
    /// <summary>HTTP 400 — No se resolvió organización (subdominio / cabecera).</summary>
    public const string OrgTenantNotResolved = "ORG_TENANT_NOT_RESOLVED";

    // ── Citas ─────────────────────────────────────────────────────────────
    /// <summary>HTTP 409 — Transición de estado de cita no permitida (§5.2.2).</summary>
    public const string AptInvalidState = "APT_INVALID_STATE";

    /// <summary>HTTP 409 — Hueco no disponible u overlap.</summary>
    public const string AptSlotUnavailable = "APT_SLOT_UNAVAILABLE";

    // ── Pagos ─────────────────────────────────────────────────────────────
    /// <summary>HTTP 402/422 — Pasarela rechaza; opcionalmente código Redsys en details (sin datos PCI).</summary>
    public const string PayRedsysDeclined = "PAY_REDSYS_DECLINED";

    // ── Clientes ──────────────────────────────────────────────────────────
    /// <summary>HTTP 403 — Cliente bloqueado para reservar.</summary>
    public const string CustBlocked = "CUST_BLOCKED";
}