using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

/// <summary>
/// Huecos libres de la agenda (vol. 1 §3.1.5 y §5.1, RA-869d7f4rd).
///
/// Controlador propio y no parte del futuro `AppointmentsController`
/// (RA-869d7f519): la disponibilidad es una consulta de solo lectura que no
/// crea ni modifica citas, y separarla evita que esta tarea y la de endpoints
/// se pisen el mismo fichero.
///
/// Autorización: basta con estar autenticado, **Customer incluido**, que es el
/// criterio del módulo y el mismo del catálogo — la clienta necesita ver los
/// huecos para poder reservar. Los huecos de un empleado no son dato personal
/// de nadie.
/// </summary>
[ApiController]
[Route("api/v1/appointments/availability")]
[Authorize]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilityController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    /// <summary>
    /// Huecos libres de un empleado en una fecha para una cita de la duración
    /// pedida. Los tres parámetros son obligatorios.
    ///
    /// Un día sin horario, cubierto por una ausencia, lleno de citas o ya
    /// pasado devuelve `slots: []` con 200: no tener huecos no es un error.
    /// Empleado inexistente o de baja, 404.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] int? employeeId,
        [FromQuery] DateOnly? date,
        [FromQuery] int? durationMinutes,
        CancellationToken cancellationToken = default)
    {
        // Los tres llegan como nullable y se comprueban aquí para que faltar un
        // parámetro salga con envelope. Si vienen con un valor malformado
        // ("date=ayer") el 400 lo emite el model binding sin envelope: es deuda
        // conocida y transversal (RA-869f1k17q), no de este endpoint.
        var missing = new[]
        {
            employeeId is null ? "employeeId" : null,
            date is null ? "date" : null,
            durationMinutes is null ? "durationMinutes" : null,
        }
            .OfType<string>()
            .Select(field => new ApiErrorDetail
            {
                Field = field,
                Code = "REQUIRED",
                Message = "Es obligatorio.",
            })
            .ToList();

        if (missing.Count > 0)
        {
            return BadRequest(ApiResponse.Fail(
                ErrorCodes.GenValidationFailed,
                "La petición no supera las validaciones.",
                missing,
                Meta));
        }

        var result = await _availabilityService.GetAvailableSlotsAsync(
            employeeId!.Value, date!.Value, durationMinutes!.Value, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    private ApiMeta Meta => ApiMeta.Create(HttpContext.TraceIdentifier);

    /// <summary>
    /// Traduce el código de negocio al status HTTP. Un código sin mapear sale
    /// como 500 a propósito: es un fallo del servidor.
    /// </summary>
    private IActionResult FromFailure<T>(Result<T> result)
    {
        var statusCode = result.ErrorCode switch
        {
            ErrorCodes.GenValidationFailed => StatusCodes.Status400BadRequest,
            ErrorCodes.OrgTenantNotResolved => StatusCodes.Status400BadRequest,
            ErrorCodes.GenForbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.GenNotFound => StatusCodes.Status404NotFound,
            ErrorCodes.AptSlotUnavailable => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return StatusCode(statusCode, ApiResponse.Fail(
            result.ErrorCode!,
            result.ErrorMessage!,
            result.ErrorDetails,
            Meta));
    }
}
