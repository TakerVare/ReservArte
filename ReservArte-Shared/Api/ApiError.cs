namespace ReservArte.Shared.Api;

/// <summary>
/// Objeto error del envelope cuando success === false (volumen 1 §5.1.1).
/// </summary>
public class ApiError
{
    /// <summary>
    /// Código de error de aplicación del catálogo §5.1.2 (ver <see cref="ErrorCodes"/>).
    /// Estable para ramificar en cliente; NO confundir con el código HTTP.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>Mensaje legible (internacionalizable en el futuro según Accept-Language).</summary>
    public required string Message { get; init; }

    /// <summary>
    /// Opcional: errores de validación por campo (array de <see cref="ApiErrorDetail"/>),
    /// códigos de pasarela, etc.
    /// </summary>
    public object? Details { get; init; }
}

/// <summary>
/// Detalle de validación por campo para error.details cuando
/// error.code = GEN_VALIDATION_FAILED (convención volumen 1 §5.1.1).
/// </summary>
public class ApiErrorDetail
{
    public required string Field { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
}