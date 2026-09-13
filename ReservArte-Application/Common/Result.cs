namespace ReservArte.Application.Common;

/// <summary>
/// Resultado de una operación de negocio: éxito con datos, o fallo con un
/// código del catálogo error.code (§5.1.2) que el controlador traduce al
/// envelope. Evita usar excepciones como control de flujo.
///
/// Es el equivalente general de `AuthResult&lt;T&gt;`, que nació acotado al
/// módulo de autenticación. No se unifican aquí para no tocar el camino de
/// auth en esta tarea; unificarlos queda como deuda anotada en el PR.
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
