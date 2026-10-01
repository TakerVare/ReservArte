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
public class AvailabilityController : ApiControllerBase
{
    private readonly IAvailabilityService _availabilityService;
    private readonly IServiceAvailabilityService _serviceAvailability;

    public AvailabilityController(
        IAvailabilityService availabilityService, IServiceAvailabilityService serviceAvailability)
    {
        _availabilityService = availabilityService;
        _serviceAvailability = serviceAvailability;
    }

    /// <summary>
    /// Huecos de un servicio en una fecha, agrupados por los empleados que lo prestan
    /// (H-45): un empleado sin el servicio o sin huecos no sale. Fuera de la ventana de
    /// reserva del rol (6 semanas la clienta, 10 el personal) la lista va vacía; la
    /// respuesta trae la ventana. Servicio inexistente o de baja, 404.
    /// </summary>
    [HttpGet("by-service")]
    [ProducesResponseType(typeof(ApiResponse<ServiceAvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceSlots(
        [FromQuery] int? serviceId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var missing = Missing(("serviceId", serviceId is null), ("date", date is null));
        if (missing is not null)
        {
            return missing;
        }

        var result = await _serviceAvailability.GetSlotsAsync(serviceId!.Value, date!.Value, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Días del intervalo [from, to] (62 días como mucho) en los que algún empleado
    /// tiene hueco para el servicio (H-45), recortados a la ventana de reserva del rol:
    /// es lo que marca el calendario. Servicio inexistente o de baja, 404.
    /// </summary>
    [HttpGet("days")]
    [ProducesResponseType(typeof(ApiResponse<ServiceAvailableDaysResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceDays(
        [FromQuery] int? serviceId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var missing = Missing(("serviceId", serviceId is null), ("from", from is null), ("to", to is null));
        if (missing is not null)
        {
            return missing;
        }

        var result = await _serviceAvailability.GetAvailableDaysAsync(
            serviceId!.Value, from!.Value, to!.Value, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    private IActionResult? Missing(params (string Field, bool IsMissing)[] parameters)
    {
        var missing = parameters
            .Where(p => p.IsMissing)
            .Select(p => new ApiErrorDetail { Field = p.Field, Code = "REQUIRED", Message = "Es obligatorio." })
            .ToList();

        return missing.Count == 0
            ? null
            : Failure(ErrorCodes.GenValidationFailed, "La petición no supera las validaciones.", missing);
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
            return Failure(ErrorCodes.GenValidationFailed,
                "La petición no supera las validaciones.",
                missing);
        }

        var result = await _availabilityService.GetAvailableSlotsAsync(
            employeeId!.Value, date!.Value, durationMinutes!.Value, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }
}
