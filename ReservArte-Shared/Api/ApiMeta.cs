namespace ReservArte.Shared.Api;

/// <summary>
/// Metadatos transversales del envelope (volumen 1 §5.1.1).
/// </summary>
public class ApiMeta
{
    /// <summary>Identificador único de la petición (correlación logs / soporte).</summary>
    public string? RequestId { get; init; }

    /// <summary>Momento de la respuesta en ISO-8601 UTC.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Versión de API expuesta.</summary>
    public string Version { get; init; } = "v1";

    /// <summary>Solo en listas paginadas; null en el resto.</summary>
    public ApiPagination? Pagination { get; init; }

    public static ApiMeta Create(string requestId, ApiPagination? pagination = null) => new()
    {
        RequestId = requestId,
        Pagination = pagination
    };
}

/// <summary>Totales de paginación para meta.pagination (los items van en data).</summary>
public class ApiPagination
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}