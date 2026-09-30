using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

/// <summary>
/// CRUD de clientes (vol. 1 §3.1.3 y §5.1, RA-869d7f3bt).
///
/// Autorización en dos niveles, que se suman: la clase deja entrar a todo el
/// personal (Admin, Manager y Employee: una Employee atiende a sus clientes y
/// necesita consultarlos), y las escrituras exigen además Admin o Manager. Así
/// una Employee no puede cambiar el email de acceso de una clienta ni darla de
/// baja. Una cuenta Customer recibe 403 en todo el módulo. Las reglas que
/// dependen de los datos (cuenta de personal, conflicto de email) las aplica
/// CustomerService.
/// </summary>
[ApiController]
[Route("api/v1/customers")]
[Authorize(Roles = StaffRoles)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
public class CustomersController : ApiControllerBase
{
    /// <summary>Todo el personal del centro: acceso de lectura.</summary>
    public const string StaffRoles = Roles.Admin + "," + Roles.Manager + "," + Roles.Employee;

    /// <summary>Gestión: alta, edición, baja y reactivación.</summary>
    public const string ManagementRoles = Roles.Admin + "," + Roles.Manager;

    private readonly ICustomerService _customerService;
    private readonly IAppointmentBookingService _appointments;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<UpdateCustomerRequest> _updateValidator;
    private readonly IValidator<CreateCustomerNoteRequest> _noteValidator;
    private readonly IValidator<RecordAllergyTestRequest> _allergyTestValidator;

    public CustomersController(
        ICustomerService customerService,
        IAppointmentBookingService appointments,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<UpdateCustomerRequest> updateValidator,
        IValidator<CreateCustomerNoteRequest> noteValidator,
        IValidator<RecordAllergyTestRequest> allergyTestValidator)
    {
        _customerService = customerService;
        _appointments = appointments;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _noteValidator = noteValidator;
        _allergyTestValidator = allergyTestValidator;
    }

    /// <summary>
    /// Lista paginada. `search` busca en nombre, apellidos y email. Sin
    /// `isActive` devuelve solo los activos (la baja es lógica). `pageSize` se
    /// acota a 100; los valores efectivos vuelven en `meta.pagination`.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ApiItems<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool? isBlocked,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new CustomerFilter
        {
            Search = search,
            Category = category,
            IsBlocked = isBlocked,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _customerService.GetPagedAsync(filter, cancellationToken);

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
            new ApiItems<CustomerDto> { Items = paged.Items },
            ApiMeta.Create(HttpContext.TraceIdentifier, pagination)));
    }

    /// <summary>Perfil completo: ficha con consentimientos, alergias y notas vigentes.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetByIdAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Historial de citas de la clienta (RA-869f2gn91): activas en cualquier estado, de
    /// la más reciente a la más antigua, con sus líneas. `pageSize` se acota a 100.
    /// </summary>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(ApiResponse<ApiItems<AppointmentDetailDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _appointments.GetCustomerHistoryAsync(id, page, pageSize, cancellationToken);
        if (!result.Success)
        {
            return FromFailure(result);
        }

        var history = result.Data!;
        var pagination = new ApiPagination
        {
            Page = history.Page,
            PageSize = history.PageSize,
            TotalCount = history.TotalCount,
            TotalPages = history.TotalPages,
        };

        return Ok(ApiResponse.Ok(
            new ApiItems<AppointmentDetailDto> { Items = history.Items },
            ApiMeta.Create(HttpContext.TraceIdentifier, pagination)));
    }

    /// <summary>
    /// Registra la última prueba de alergia de la clienta (RA-869f9cu2x): todo el
    /// personal, porque la hace quien atiende. Fecha con zona y no futura. Los
    /// servicios que la exigen avisan en la ficha de la cita si falta o no llega a
    /// tiempo; no bloquean la reserva.
    /// </summary>
    [HttpPut("{id:int}/allergy-test")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordAllergyTest(
        int id, RecordAllergyTestRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_allergyTestValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _customerService.RecordAllergyTestAsync(id, request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Alta con los consentimientos aceptados (`grantedConsents`, con
    /// `data_processing` obligatorio). Si el email es de una cuenta del centro
    /// sin ficha de cliente, se le añade la ficha; una cuenta nueva recibe la
    /// invitación para crear su contraseña.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_createValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _customerService.CreateAsync(request, cancellationToken);

        return result.Success
            ? CreatedAtAction(
                nameof(GetById), new { id = result.Data!.Id }, ApiResponse.Ok(result.Data, Meta))
            : FromFailure(result);
    }

    /// <summary>
    /// Edición de la ficha. En una cuenta solo de cliente se propaga a la
    /// cuenta de acceso; en una de personal no, y cambiar su email es 403.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_updateValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _customerService.UpdateAsync(id, request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Baja lógica de la ficha. Idempotente. A diferencia de Empleados, no
    /// bloquea la cuenta.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _customerService.DeactivateAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>Deshace una baja de la ficha. Idempotente.</summary>
    [HttpPost("{id:int}/reactivate")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _customerService.ReactivateAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    // ── Notas internas (RA-869d7f3fw) ─────────────────────────────────────

    /// <summary>
    /// Añade una nota interna. Todo el personal puede escribirla, pero la firma
    /// su ficha de empleado activa: sin ella, 403. Las notas vigentes se leen en
    /// el perfil, al que apunta el Location.
    /// </summary>
    [HttpPost("{id:int}/notes")]
    [ProducesResponseType(typeof(ApiResponse<CustomerNoteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddNote(
        int id, CreateCustomerNoteRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_noteValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _customerService.AddNoteAsync(id, request, cancellationToken);

        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id }, ApiResponse.Ok(result.Data!, Meta))
            : FromFailure(result);
    }

    /// <summary>
    /// Retira una nota (baja lógica). Idempotente. Solo su autora, un Admin o un
    /// Manager.
    /// </summary>
    [HttpDelete("{id:int}/notes/{noteId:int}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerNoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNote(int id, int noteId, CancellationToken cancellationToken)
    {
        var result = await _customerService.DeleteNoteAsync(id, noteId, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }
}
