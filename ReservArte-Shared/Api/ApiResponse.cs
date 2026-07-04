namespace ReservArte.Shared.Api;

/// <summary>
/// Envelope estándar de todas las respuestas JSON de la API pública
/// (volumen 1 §5.1.1): { success, data, error, meta }.
/// Excepciones sin envelope: webhooks Redsys y health checks.
/// </summary>
public class ApiResponse<T>
{
    /// <summary>True si la operación se completó según el contrato del endpoint.</summary>
    public bool Success { get; init; }

    /// <summary>Payload de negocio en éxito; null en error (salvo datos parciales documentados).</summary>
    public T? Data { get; init; }

    /// <summary>Objeto de error cuando Success es false; null en éxito.</summary>
    public ApiError? Error { get; init; }

    /// <summary>Metadatos transversales (requestId, timestamp, version, pagination).</summary>
    public ApiMeta? Meta { get; init; }

    public static ApiResponse<T> Ok(T data, ApiMeta? meta = null) => new()
    {
        Success = true,
        Data = data,
        Error = null,
        Meta = meta ?? new ApiMeta()
    };

    public static ApiResponse<T> Fail(string code, string message,
        object? details = null, ApiMeta? meta = null) => new()
    {
        Success = false,
        Data = default,
        Error = new ApiError { Code = code, Message = message, Details = details },
        Meta = meta ?? new ApiMeta()
    };
}

/// <summary>
/// Fábricas no genéricas para mejorar la ergonomía:
/// ApiResponse.Ok(data) infiere T; ApiResponse.Fail(...) para errores sin payload.
/// </summary>
public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, ApiMeta? meta = null) =>
        ApiResponse<T>.Ok(data, meta);

    public static ApiResponse<object> Fail(string code, string message,
        object? details = null, ApiMeta? meta = null) =>
        ApiResponse<object>.Fail(code, message, details, meta);
}