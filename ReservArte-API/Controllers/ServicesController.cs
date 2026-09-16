using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Services;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

/// <summary>
/// Catálogo de servicios (vol. 1 §3.1.4 y §5.1, RA-869d7f42u).
///
/// Autorización en dos niveles, que se suman: la clase pide solo estar
/// autenticado, porque el catálogo lo necesita también una cuenta Customer para
/// elegir servicio al reservar y no es dato personal ni sensible; las escrituras
/// exigen además Admin o Manager. Es la diferencia deliberada con Clientes y
/// Empleados, donde el rol Customer recibe 403 en todo el módulo.
///
/// Las reglas que dependen de los datos (categoría inexistente en el centro,
/// servicio de otra organización) las aplica ServiceCatalogService.
/// </summary>
[ApiController]
[Route("api/v1/services")]
[Authorize]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
public class ServicesController : ControllerBase
{
    /// <summary>Gestión del catálogo: alta, edición, baja y reactivación.</summary>
    public const string ManagementRoles = Roles.Admin + "," + Roles.Manager;

    private readonly IServiceCatalogService _catalogService;
    private readonly IValidator<CreateServiceRequest> _createValidator;
    private readonly IValidator<UpdateServiceRequest> _updateValidator;
    private readonly IValidator<CreateServiceCategoryRequest> _createCategoryValidator;
    private readonly IValidator<UpdateServiceCategoryRequest> _updateCategoryValidator;
    private readonly IValidator<CreateServiceVariationRequest> _createVariationValidator;
    private readonly IValidator<UpdateServiceVariationRequest> _updateVariationValidator;
    private readonly IValidator<UpsertServicePricingRequest> _pricingValidator;

    public ServicesController(
        IServiceCatalogService catalogService,
        IValidator<CreateServiceRequest> createValidator,
        IValidator<UpdateServiceRequest> updateValidator,
        IValidator<CreateServiceCategoryRequest> createCategoryValidator,
        IValidator<UpdateServiceCategoryRequest> updateCategoryValidator,
        IValidator<CreateServiceVariationRequest> createVariationValidator,
        IValidator<UpdateServiceVariationRequest> updateVariationValidator,
        IValidator<UpsertServicePricingRequest> pricingValidator)
    {
        _catalogService = catalogService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _createCategoryValidator = createCategoryValidator;
        _updateCategoryValidator = updateCategoryValidator;
        _createVariationValidator = createVariationValidator;
        _updateVariationValidator = updateVariationValidator;
        _pricingValidator = pricingValidator;
    }

    /// <summary>
    /// Lista paginada. `search` busca en nombre y descripción. Sin `isActive`
    /// devuelve solo los activos (la baja es lógica). `pageSize` se acota a 100;
    /// los valores efectivos vuelven en `meta.pagination`.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ApiItems<ServiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new ServiceFilter
        {
            Search = search,
            CategoryId = categoryId,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _catalogService.GetPagedAsync(filter, cancellationToken);

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
            new ApiItems<ServiceDto> { Items = paged.Items },
            ApiMeta.Create(HttpContext.TraceIdentifier, pagination)));
    }

    /// <summary>
    /// Categorías del centro, en su orden de presentación. Sin `isActive`
    /// devuelve todas: el formulario de alta necesita ver también las retiradas
    /// para no perder la clasificación de un servicio ya guardado.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<ApiItems<ServiceCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        var result = await _catalogService.GetCategoriesAsync(isActive, cancellationToken);

        return result.Success
            ? Ok(ApiResponse.Ok(new ApiItems<ServiceCategoryDto> { Items = result.Data! }, Meta))
            : FromFailure(result);
    }

    /// <summary>Servicio con sus variaciones y tarifas por nivel vigentes.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _catalogService.GetByIdAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Alta de un servicio. Una categoría que no exista en el centro es 400
    /// (`field = categoryId`), no 404: el recurso que se crea es el servicio.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_createValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _catalogService.CreateAsync(request, cancellationToken);

        return result.Success
            ? CreatedAtAction(
                nameof(GetById), new { id = result.Data!.Id }, ApiResponse.Ok(result.Data, Meta))
            : FromFailure(result);
    }

    /// <summary>
    /// Edición. No cambia el estado de baja: para eso están DELETE y
    /// reactivate, igual que en Empleados y Clientes.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id, UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_updateValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _catalogService.UpdateAsync(id, request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Baja lógica, idempotente. El servicio deja de ofrecerse pero no
    /// desaparece: las citas ya cerradas seguirán apuntando a él.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _catalogService.DeactivateAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>Deshace una baja. Idempotente.</summary>
    [HttpPost("{id:int}/reactivate")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _catalogService.ReactivateAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    // ── Categorías (RA-869f2wtrk) ─────────────────────────────────────────

    /// <summary>
    /// Alta de categoría. El `Location` apunta a la lista de categorías: no hay
    /// endpoint de categoría por id, porque se consumen siempre como conjunto.
    /// </summary>
    [HttpPost("categories")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory(
        CreateServiceCategoryRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_createCategoryValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _catalogService.CreateCategoryAsync(request, cancellationToken);

        return result.Success
            ? CreatedAtAction(nameof(GetCategories), null, ApiResponse.Ok(result.Data!, Meta))
            : FromFailure(result);
    }

    /// <summary>Edición de categoría. No cambia su estado de baja.</summary>
    [HttpPut("categories/{categoryId:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(
        int categoryId, UpdateServiceCategoryRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_updateCategoryValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _catalogService.UpdateCategoryAsync(categoryId, request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Baja lógica de categoría, idempotente. **Se permite aunque tenga
    /// servicios:** ninguno queda sin clasificar, porque la fila no se borra y
    /// `GET /categories` sin filtro sigue devolviéndola.
    /// </summary>
    [HttpDelete("categories/{categoryId:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateCategory(
        int categoryId, CancellationToken cancellationToken)
    {
        var result = await _catalogService.DeactivateCategoryAsync(categoryId, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>Deshace la baja de una categoría. Idempotente.</summary>
    [HttpPost("categories/{categoryId:int}/reactivate")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReactivateCategory(
        int categoryId, CancellationToken cancellationToken)
    {
        var result = await _catalogService.ReactivateCategoryAsync(categoryId, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    // ── Variaciones (RA-869f2wtrk) ────────────────────────────────────────

    /// <summary>
    /// Añade una variante al servicio. Una que dejaría la duración resultante en
    /// cero o negativa es 400 (`field = durationModifier`). El `Location` apunta
    /// al detalle del servicio, que es donde se leen las variaciones.
    /// </summary>
    [HttpPost("{id:int}/variations")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceVariationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddVariation(
        int id, CreateServiceVariationRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_createVariationValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _catalogService.AddVariationAsync(id, request, cancellationToken);

        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id }, ApiResponse.Ok(result.Data!, Meta))
            : FromFailure(result);
    }

    /// <summary>Edita una variante del servicio.</summary>
    [HttpPut("{id:int}/variations/{variationId:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceVariationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVariation(
        int id, int variationId, UpdateServiceVariationRequest request,
        CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_updateVariationValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _catalogService.UpdateVariationAsync(
            id, variationId, request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Retira una variante (baja lógica). Idempotente: repetir la llamada vuelve
    /// a devolver 200, porque la variación retirada se sigue localizando.
    /// </summary>
    [HttpDelete("{id:int}/variations/{variationId:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServiceVariationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVariation(
        int id, int variationId, CancellationToken cancellationToken)
    {
        var result = await _catalogService.DeleteVariationAsync(id, variationId, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    // ── Tarifas por nivel (RA-869f2wtrk) ──────────────────────────────────

    /// <summary>
    /// Crea o actualiza la tarifa del nivel. Es `PUT` y no `POST` porque el
    /// nivel es la clave natural de la tarifa y solo puede haber una vigente por
    /// servicio y nivel: así la operación es idempotente y no choca con el
    /// índice único. Un nivel fuera del catálogo es 400 (`field = employeeLevel`).
    /// </summary>
    [HttpPut("{id:int}/pricings/{employeeLevel}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServicePricingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPricing(
        int id, string employeeLevel, UpsertServicePricingRequest request,
        CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_pricingValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _catalogService.SetPricingAsync(
            id, employeeLevel, request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Retira la tarifa vigente del nivel. **No es idempotente**: sin tarifa
    /// vigente devuelve 404, porque el recurso es precisamente la vigente y una
    /// retirada ya no se puede direccionar por su nivel.
    /// </summary>
    [HttpDelete("{id:int}/pricings/{employeeLevel}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServicePricingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePricing(
        int id, string employeeLevel, CancellationToken cancellationToken)
    {
        var result = await _catalogService.DeletePricingAsync(id, employeeLevel, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    // Cuarta réplica de los de EmployeesController; se unificarán con RA-869f17y6k.

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
    /// «Variations[1].Name», y el contrato expone los campos en camelCase.
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
