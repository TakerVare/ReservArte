namespace ReservArte.Application.Common;

/// <summary>
/// Resultado de una operación de negocio: éxito con datos, o fallo con un
/// código del catálogo error.code (§5.1.2) que el controlador traduce al
/// envelope. Evita usar excepciones como control de flujo.
///
/// Es el único tipo de resultado: `AuthResult&lt;T&gt;`, idéntico, se retiró al unificar
/// los dos en RA-869f6r81n.
/// </summary>
public class Result<T>
{
    public bool Success { get; private init; }

    public T? Data { get; private init; }

    public string? ErrorCode { get; private init; }

    public string? ErrorMessage { get; private init; }

    public object? ErrorDetails { get; private init; }

    public static Result<T> Ok(T data) => new()
    {
        Success = true,
        Data = data,
    };

    public static Result<T> Fail(string errorCode, string errorMessage, object? details = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        ErrorDetails = details,
    };
}
