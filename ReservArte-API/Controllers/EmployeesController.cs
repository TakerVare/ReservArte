using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

/// <summary>
/// CRUD de empleados (vol. 1 §3.1.2 y §5.1, RA-869d7ezz4).
///
/// La autorización está repartida a propósito: el atributo decide quién entra
/// al módulo (Admin y Manager) y EmployeeService aplica las reglas que
/// dependen de los datos —el rol del empleado afectado, o si es uno mismo—,
/// que el atributo no puede ver. Ambas denegaciones salen como 403
/// GEN_FORBIDDEN con envelope.
/// </summary>
[ApiController]
[Route("api/v1/employees")]
[Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IValidator<CreateEmployeeRequest> _createValidator;
    private readonly IValidator<UpdateEmployeeRequest> _updateValidator;
    private readonly IValidator<UpdateAvailabilityRequest> _availabilityValidator;
    private readonly IValidator<CreateEmployeeExceptionRequest> _exceptionValidator;

    public EmployeesController(
        IEmployeeService employeeService,
        IValidator<CreateEmployeeRequest> createValidator,
        IValidator<UpdateEmployeeRequest> updateValidator,
        IValidator<UpdateAvailabilityRequest> availabilityValidator,
        IValidator<CreateEmployeeExceptionRequest> exceptionValidator)
    {
        _employeeService = employeeService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _availabilityValidator = availabilityValidator;
        _exceptionValidator = exceptionValidator;
    }

    /// <summary>
    /// Lista paginada. Sin `isActive` devuelve solo los activos (la baja es
    /// lógica). `pageSize` se acota a 100; los valores efectivos vuelven en
    /// `meta.pagination`.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ApiItems<EmployeeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? search,
        [FromQuery] string? rol,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new EmployeeFilter
        {
            Search = search,
            Rol = rol,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _employeeService.GetPagedAsync(filter, cancellationToken);

        if (!result.Success)
        {
            return FromFailure(result);
        }

        var paged = result.Data!;
        var pagination = new ApiPagination
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages,
        };

        return Ok(ApiResponse.Ok(
            new ApiItems<EmployeeDto> { Items = paged.Items },
            ApiMeta.Create(HttpContext.TraceIdentifier, pagination)));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetByIdAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Alta. La cuenta nace sin contraseña: el empleado la establece con
    /// `/auth/forgot-password`. Solo un Admin puede crear otro Admin.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_createValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _employeeService.CreateAsync(request, cancellationToken);

        return result.Success
            ? CreatedAtAction(
                nameof(GetById), new { id = result.Data!.Id }, ApiResponse.Ok(result.Data, Meta))
            : FromFailure(result);
    }

    /// <summary>
    /// Edición completa de la ficha, propagada a la cuenta de acceso. Nadie
    /// puede cambiarse su propio rol, y un Manager no puede tocar a un Admin
    /// ni promocionar a nadie a Admin.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_updateValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _employeeService.UpdateAsync(id, request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Baja lógica: desactiva la ficha y bloquea la cuenta (RA-869f180e5).
    /// Idempotente. Nadie puede darse de baja a sí mismo.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _employeeService.DeactivateAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>Deshace una baja: reactiva la ficha y retira el bloqueo de la cuenta. Idempotente.</summary>
    [HttpPost("{id:int}/reactivate")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _employeeService.ReactivateAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    // ── Disponibilidad (RA-869d7f01b) ─────────────────────────────────────

    /// <summary>
    /// Horario semanal del empleado y sus ausencias. `from`/`to` acotan las
    /// ausencias (UTC); por defecto, desde hoy y 90 días. El rango realmente
    /// aplicado vuelve en la respuesta.
    /// </summary>
    [HttpGet("{id:int}/availability")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeAvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailability(
        int id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetAvailabilityAsync(id, from, to, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Reemplaza el horario semanal completo: se manda la semana entera, no un
    /// parche. Una lista vacía deja al empleado sin horario.
    /// </summary>
    [HttpPut("{id:int}/availability")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeAvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceAvailability(
        int id, UpdateAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_availabilityValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _employeeService.ReplaceAvailabilityAsync(id, request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>Registra una ausencia puntual (vacaciones, baja, formación…).</summary>
    [HttpPost("{id:int}/exceptions")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeExceptionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddException(
        int id, CreateEmployeeExceptionRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_exceptionValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _employeeService.AddExceptionAsync(id, request, cancellationToken);

        return result.Success
            ? CreatedAtAction(
                nameof(GetAvailability), new { id }, ApiResponse.Ok(result.Data!, Meta))
            : FromFailure(result);
    }

    /// <summary>Retira una ausencia (baja lógica). Idempotente.</summary>
    [HttpDelete("{id:int}/exceptions/{exceptionId:int}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeExceptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteException(
        int id, int exceptionId, CancellationToken cancellationToken)
    {
        var result = await _employeeService.DeleteExceptionAsync(id, exceptionId, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    // ValidateAsync y ToCamelCase replican los de AuthController, que trabaja
    // con AuthResult en vez de Result: se unificarán con RA-869f17y6k.

    private ApiMeta Meta => ApiMeta.Create(HttpContext.TraceIdentifier);

    private async Task<IActionResult?> ValidateAsync<T>(
        IValidator<T> validator, T request, CancellationToken cancellationToken)
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

        return BadRequest(ApiResponse.Fail(
            ErrorCodes.GenValidationFailed,
            "La petición no supera las validaciones.",
            details,
            Meta));
    }

    /// <summary>
    /// Traduce el código de negocio al status HTTP. Un código sin mapear sale
    /// como 500 a propósito: es un fallo del servidor, y un 400 lo disfrazaría
    /// de error del cliente.
    /// </summary>
    private IActionResult FromFailure<T>(Result<T> result)
    {
        var statusCode = result.ErrorCode switch
        {
            ErrorCodes.GenValidationFailed => StatusCodes.Status400BadRequest,
            ErrorCodes.OrgTenantNotResolved => StatusCodes.Status400BadRequest,
            ErrorCodes.GenForbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.GenNotFound => StatusCodes.Status404NotFound,
            ErrorCodes.GenConflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return StatusCode(statusCode, ApiResponse.Fail(
            result.ErrorCode!,
            result.ErrorMessage!,
            result.ErrorDetails,
            Meta));
    }

    /// <summary>
    /// camelCase en CADA tramo de la ruta: FluentValidation devuelve rutas
    /// anidadas como «WeeklySchedule[0].DayOfWeek», y el contrato expone los
    /// nombres de campo en camelCase, también los de dentro de una colección.
    /// </summary>
    private static string ToCamelCase(string propertyName) =>
        string.IsNullOrEmpty(propertyName)
            ? propertyName
            : string.Join('.', propertyName.Split('.').Select(CamelCaseSegment));

    private static string CamelCaseSegment(string segment) =>
        string.IsNullOrEmpty(segment)
            ? segment
            : char.ToLowerInvariant(segment[0]) + segment[1..];
}
