using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Application.Common;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

/// <summary>
/// Base de los controladores de la API (RA-869f6r81n): la respuesta de error
/// común, la validación con FluentValidation y el <c>meta</c> del envelope. Antes
/// cada controlador tenía su copia de estas piezas.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary><c>meta</c> del envelope con el identificador de la petición.</summary>
    protected ApiMeta Meta => ApiMeta.Create(HttpContext.TraceIdentifier);

    /// <summary>
    /// Respuesta de error de un <see cref="Result{T}"/> fallido, con el status que
    /// fija <see cref="ErrorStatusCodes"/> para su código.
    /// </summary>
    protected IActionResult FromFailure<T>(Result<T> result) =>
        Failure(result.ErrorCode!, result.ErrorMessage!, result.ErrorDetails);

    /// <summary>Respuesta de error con envelope y el status que corresponde al código.</summary>
    protected IActionResult Failure(string errorCode, string message, object? details = null) =>
        StatusCode(ErrorStatusCodes.For(errorCode), ApiResponse.Fail(errorCode, message, details, Meta));

    /// <summary>
    /// Valida la petición. Devuelve null si es válida, o el 400
    /// GEN_VALIDATION_FAILED con un detalle por campo en camelCase.
    /// </summary>
    protected async Task<IActionResult?> ValidateAsync<T>(
        IValidator<T> validator, T request, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);

        if (validation.IsValid)
        {
            return null;
        }

        var details = validation.Errors
            .Select(e => new ApiErrorDetail
            {
                Field = ToCamelCase(e.PropertyName),
                Code = e.ErrorCode,
                Message = e.ErrorMessage,
            })
            .ToList();

        return Failure(ErrorCodes.GenValidationFailed, "La petición no supera las validaciones.", details);
    }

    /// <summary>
    /// camelCase en CADA tramo de la ruta: FluentValidation devuelve rutas
    /// anidadas como «WeeklySchedule[0].DayOfWeek», y el contrato expone los
    /// nombres de campo en camelCase, también los de dentro de una colección.
    /// </summary>
    internal static string ToCamelCase(string propertyName) =>
        string.IsNullOrEmpty(propertyName)
            ? propertyName
            : string.Join('.', propertyName.Split('.').Select(CamelCaseSegment));

    private static string CamelCaseSegment(string segment) =>
        string.IsNullOrEmpty(segment)
            ? segment
            : char.ToLowerInvariant(segment[0]) + segment[1..];
}
