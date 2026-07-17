namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Resultado de una operación de autenticación: éxito con datos, o fallo
/// con código del catálogo error.code (§5.1.2) que el controlador mapea
/// al envelope. Evita usar excepciones como control de flujo.
/// </summary>
public class AuthResult<T>
{
    public bool Success { get; private init; }

    public T? Data { get; private init; }

    public string? ErrorCode { get; private init; }

    public string? ErrorMessage { get; private init; }

    public object? ErrorDetails { get; private init; }

    public static AuthResult<T> Ok(T data) => new()
    {
        Success = true,
        Data = data,
    };

    public static AuthResult<T> Fail(string errorCode, string errorMessage, object? details = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        ErrorDetails = details,
    };
}