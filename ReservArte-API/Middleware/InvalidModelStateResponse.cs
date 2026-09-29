using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ReservArte.API.Controllers;
using ReservArte.Shared.Api;

namespace ReservArte.API.Middleware;

/// <summary>
/// Respuesta de los 400 de model binding de <c>[ApiController]</c> (RA-869f1k17q):
/// el mismo envelope <c>GEN_VALIDATION_FAILED</c> que la validación con
/// FluentValidation, en vez del <c>ProblemDetails</c> por defecto. Salta antes de la
/// acción: JSON mal formado, cuerpo vacío o un valor que no se puede convertir.
///
/// Los mensajes del framework (en inglés y con detalles internos del parser, como
/// «LineNumber» o «BytePositionInLine») no se reenvían: cada detalle lleva un
/// mensaje fijo en español según su tipo.
/// </summary>
public static class InvalidModelStateResponse
{
    /// <summary>JSON mal formado, o un valor con un tipo que no es el esperado.</summary>
    public const string InvalidJsonCode = "InvalidJson";

    /// <summary>Falta el cuerpo, o no se pudo leer.</summary>
    public const string MissingBodyCode = "MissingBody";

    /// <summary>Un parámetro de ruta o de consulta que no se puede convertir (<c>page=abc</c>).</summary>
    public const string InvalidFormatCode = "InvalidFormat";

    /// <summary>Nombre de campo con el que se señala el cuerpo entero.</summary>
    public const string BodyField = "body";

    public static IActionResult Create(ActionContext context)
    {
        // El parámetro que recibe el cuerpo («request») aparece en ModelState con un
        // «The request field is required.» cada vez que el cuerpo falla: no es un
        // campo del contrato, así que se trata como error del cuerpo entero.
        var bodyParameters = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var details = new List<ApiErrorDetail>();
        var bodyFailed = false;

        foreach (var (key, entry) in context.ModelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            if (key.StartsWith('$'))
            {
                details.Add(new ApiErrorDetail
                {
                    Field = FieldFromJsonPath(key),
                    Code = InvalidJsonCode,
                    Message = "El JSON está mal formado o el valor no tiene el tipo esperado.",
                });
            }
            else if (key.Length == 0 || bodyParameters.Contains(key))
            {
                bodyFailed = true;
            }
            else
            {
                details.Add(new ApiErrorDetail
                {
                    Field = ApiControllerBase.ToCamelCase(key),
                    Code = InvalidFormatCode,
                    Message = "El valor no tiene un formato válido.",
                });
            }
        }

        // Un cuerpo que falla sin decir dónde (vacío o ilegible) se señala una vez.
        if (bodyFailed && !details.Any(d => d.Code == InvalidJsonCode))
        {
            details.Insert(0, new ApiErrorDetail
            {
                Field = BodyField,
                Code = MissingBodyCode,
                Message = "Falta el cuerpo de la petición o no se ha podido leer.",
            });
        }

        return new ObjectResult(ApiResponse.Fail(
            ErrorCodes.GenValidationFailed,
            "La petición no supera las validaciones.",
            details,
            ApiMeta.Create(context.HttpContext.TraceIdentifier)))
        {
            StatusCode = ErrorStatusCodes.For(ErrorCodes.GenValidationFailed),
            ContentTypes = { "application/json" },
        };
    }

    /// <summary>
    /// Ruta JSON de System.Text.Json al nombre de campo del contrato:
    /// <c>$.weeklySchedule[0].dayOfWeek</c> → <c>weeklySchedule[0].dayOfWeek</c>;
    /// <c>$</c> (el documento entero) → <c>body</c>.
    /// </summary>
    internal static string FieldFromJsonPath(string path)
    {
        var field = path.TrimStart('$').TrimStart('.');
        return field.Length == 0 ? BodyField : ApiControllerBase.ToCamelCase(field);
    }
}
