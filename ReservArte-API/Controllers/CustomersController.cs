using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Application.Common;
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
public class CustomersController : ControllerBase
{
    /// <summary>Todo el personal del centro: acceso de lectura.</summary>
    public const string StaffRoles = Roles.Admin + "," + Roles.Manager + "," + Roles.Employee;

    /// <summary>Gestión: alta, edición, baja y reactivación.</summary>
    public const string ManagementRoles = Roles.Admin + "," + Roles.Manager;

    private readonly ICustomerService _customerService;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<UpdateCustomerRequest> _updateValidator;
    private readonly IValidator<CreateCustomerNoteRequest> _noteValidator;

    public CustomersController(
        ICustomerService customerService,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<UpdateCustomerRequest> updateValidator,
        IValidator<CreateCustomerNoteRequest> noteValidator)
    {
        _customerService = customerService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _noteValidator = noteValidator;
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

    // ── Helpers ───────────────────────────────────────────────────────────
    // Réplica de los de EmployeesController; se unificarán con RA-869f17y6k.

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
    /// camelCase en cada tramo de la ruta: FluentValidation devuelve rutas como
    /// «GrantedConsents[1]», y el contrato expone los campos en camelCase.
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
