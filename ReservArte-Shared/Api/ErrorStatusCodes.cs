using System.Net;

namespace ReservArte.Shared.Api;

/// <summary>
/// Mapa único código de error → status HTTP (RA-869f6r81n). Lo usan todos los
/// controladores a través de <c>ApiControllerBase</c>; antes cada uno tenía su
/// copia y ya divergían (Auth mandaba a 400 lo que no conocía, Disponibilidad era
/// la única que sabía de <see cref="ErrorCodes.AptSlotUnavailable"/>).
///
/// Todo código de <see cref="ErrorCodes"/> tiene que estar aquí: un test lo exige.
/// Un código desconocido sale como 500 a propósito, porque es un fallo del
/// servidor y un 400 lo disfrazaría de error del cliente.
/// </summary>
public static class ErrorStatusCodes
{
    private static readonly Dictionary<string, HttpStatusCode> Map = new()
    {
        [ErrorCodes.GenInternalError] = HttpStatusCode.InternalServerError,
        [ErrorCodes.GenNotFound] = HttpStatusCode.NotFound,
        [ErrorCodes.GenUnauthorized] = HttpStatusCode.Unauthorized,
        [ErrorCodes.GenForbidden] = HttpStatusCode.Forbidden,
        [ErrorCodes.GenConflict] = HttpStatusCode.Conflict,
        [ErrorCodes.GenValidationFailed] = HttpStatusCode.BadRequest,
        [ErrorCodes.GenRateLimited] = HttpStatusCode.TooManyRequests,

        [ErrorCodes.AuthInvalidCredentials] = HttpStatusCode.Unauthorized,
        [ErrorCodes.AuthRefreshInvalid] = HttpStatusCode.Unauthorized,
        [ErrorCodes.AuthMfaInvalid] = HttpStatusCode.BadRequest,

        [ErrorCodes.OrgTenantNotResolved] = HttpStatusCode.BadRequest,
        [ErrorCodes.OrgTenantMismatch] = HttpStatusCode.Forbidden,

        [ErrorCodes.AptInvalidState] = HttpStatusCode.Conflict,
        [ErrorCodes.AptSlotUnavailable] = HttpStatusCode.Conflict,

        // El catálogo admite 402 o 422; 402 (Payment Required) es la convención
        // habitual de una pasarela que rechaza el cargo.
        [ErrorCodes.PayRedsysDeclined] = HttpStatusCode.PaymentRequired,

        [ErrorCodes.CustBlocked] = HttpStatusCode.Forbidden,
    };

    /// <summary>Códigos con status asignado (para el test de cobertura del catálogo).</summary>
    public static IReadOnlyCollection<string> MappedCodes => Map.Keys;

    /// <summary>Status HTTP del código; 500 si el código no está en el catálogo.</summary>
    public static int For(string? errorCode) =>
        (int)(errorCode is not null && Map.TryGetValue(errorCode, out var status)
            ? status
            : HttpStatusCode.InternalServerError);
}
