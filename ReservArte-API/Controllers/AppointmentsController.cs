using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

/// <summary>
/// Agenda y reserva de citas (vol. 1 §3.1.5 y §5.1, RA-869d7f519).
///
/// La autorización está repartida como en Empleados: el atributo decide quién
/// entra a cada acción y los servicios aplican lo que depende del dato (la
/// clienta solo ve y cancela sus citas; la ajena le da 404). La disponibilidad
/// vive en <see cref="AvailabilityController"/> (`/appointments/availability`).
/// </summary>
[ApiController]
[Route("api/v1/appointments")]
[Authorize]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
public class AppointmentsController : ApiControllerBase
{
    private const string StaffRoles = Roles.Admin + "," + Roles.Manager + "," + Roles.Employee;
    private const string ManagementRoles = Roles.Admin + "," + Roles.Manager;

    private readonly IAppointmentBookingService _booking;
    private readonly IAppointmentService _stateMachine;
    private readonly IValidator<CreateAppointmentRequest> _createValidator;
    private readonly IValidator<UpdateAppointmentRequest> _updateValidator;
    private readonly IValidator<CancelAppointmentRequest> _cancelValidator;

    public AppointmentsController(
        IAppointmentBookingService booking,
        IAppointmentService stateMachine,
        IValidator<CreateAppointmentRequest> createValidator,
        IValidator<UpdateAppointmentRequest> updateValidator,
        IValidator<CancelAppointmentRequest> cancelValidator)
    {
        _booking = booking;
        _stateMachine = stateMachine;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _cancelValidator = cancelValidator;
    }

    /// <summary>
    /// Agenda paginada, de la más reciente a la más antigua. Sin `isActive`
    /// devuelve solo las activas. Una clienta recibe solo sus citas, filtre como
    /// filtre. `pageSize` se acota a 100.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ApiItems<AppointmentSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? employeeId,
        [FromQuery] int? customerId,
        [FromQuery] string? status,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new AppointmentFilter
        {
            From = from,
            To = to,
            EmployeeId = employeeId,
            CustomerId = customerId,
            Status = status,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _booking.GetPagedAsync(filter, cancellationToken);
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
            new ApiItems<AppointmentSummaryDto> { Items = paged.Items },
            ApiMeta.Create(HttpContext.TraceIdentifier, pagination)));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _booking.GetByIdAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Alta por el personal, en estado `pending`. Fin, precio y duración los
    /// calcula el servidor. 409 `APT_SLOT_UNAVAILABLE` si el hueco no está libre;
    /// 403 `CUST_BLOCKED` si la clienta está bloqueada.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = StaffRoles)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_createValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _booking.CreateAsync(request, cancellationToken);

        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, ApiResponse.Ok(result.Data, Meta))
            : FromFailure(result);
    }

    /// <summary>
    /// Edición de una cita que no ha empezado (`pending` o `confirmed`): empleada,
    /// fecha, hora, servicios y notas. 409 `APT_INVALID_STATE` si ya empezó o está
    /// cerrada.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = StaffRoles)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id, UpdateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_updateValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _booking.UpdateAsync(id, request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>Baja lógica (Admin o Manager), idempotente. No es cancelar.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _booking.DeactivateAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    // ── Transiciones de estado (RA-869d7f4xf) ─────────────────────────────

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = StaffRoles)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Confirm(int id, CancellationToken cancellationToken) =>
        TransitionAsync(_stateMachine.ConfirmAsync(id, cancellationToken));

    [HttpPost("{id:int}/start")]
    [Authorize(Roles = StaffRoles)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Start(int id, CancellationToken cancellationToken) =>
        TransitionAsync(_stateMachine.StartAsync(id, cancellationToken));

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = StaffRoles)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Complete(int id, CancellationToken cancellationToken) =>
        TransitionAsync(_stateMachine.CompleteAsync(id, cancellationToken));

    /// <summary>No presentada: solo Admin o Manager (alimenta el contador de no-shows).</summary>
    [HttpPost("{id:int}/no-show")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public Task<IActionResult> NoShow(int id, CancellationToken cancellationToken) =>
        TransitionAsync(_stateMachine.MarkNoShowAsync(id, cancellationToken));

    /// <summary>
    /// Cancelación por el personal o por la clienta dueña (una ajena → 404). El
    /// cuerpo, con el motivo, es opcional.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        int id, [FromBody] CancelAppointmentRequest? request, CancellationToken cancellationToken)
    {
        request ??= new CancelAppointmentRequest();

        var invalid = await ValidateAsync(_cancelValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        return await TransitionAsync(_stateMachine.CancelAsync(id, request, cancellationToken));
    }

    private async Task<IActionResult> TransitionAsync(Task<Application.Common.Result<AppointmentDto>> transition)
    {
        var result = await transition;
        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }
}
