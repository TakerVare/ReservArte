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
/// Paquetes del catálogo (vol. 1 §3.1.4 y §5.1, RA-869d7f45n).
///
/// Controlador propio y no parte de `ServicesController` porque es un recurso
/// distinto (`/api/v1/service-packages`). Misma autorización que el catálogo: la
/// clase pide solo estar autenticado —una clienta necesita ver los paquetes para
/// elegir al reservar— y las escrituras exigen Admin o Manager.
/// </summary>
[ApiController]
[Route("api/v1/service-packages")]
[Authorize]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
public class ServicePackagesController : ApiControllerBase
{
    /// <summary>Gestión del catálogo: alta, edición, baja y reactivación.</summary>
    public const string ManagementRoles = Roles.Admin + "," + Roles.Manager;

    private readonly IServicePackageService _packageService;
    private readonly IValidator<CreateServicePackageRequest> _createValidator;
    private readonly IValidator<UpdateServicePackageRequest> _updateValidator;

    public ServicePackagesController(
        IServicePackageService packageService,
        IValidator<CreateServicePackageRequest> createValidator,
        IValidator<UpdateServicePackageRequest> updateValidator)
    {
        _packageService = packageService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lista paginada. `search` busca en nombre y descripción. Sin `isActive`
    /// devuelve solo los activos. `pageSize` se acota a 100; los valores
    /// efectivos vuelven en `meta.pagination`.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ApiItems<ServicePackageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new ServicePackageFilter
        {
            Search = search,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _packageService.GetPagedAsync(filter, cancellationToken);

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
            new ApiItems<ServicePackageDto> { Items = paged.Items },
            ApiMeta.Create(HttpContext.TraceIdentifier, pagination)));
    }

    /// <summary>
    /// Paquete con sus servicios, en orden. La respuesta añade el desglose
    /// calculado: suma de los precios sueltos, ahorro y duración total.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ServicePackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _packageService.GetByIdAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Alta con su composición. Un `serviceId` que no exista en el centro es 400
    /// con el índice de la línea (`field = items[i].serviceId`), no 404: el
    /// recurso que se crea es el paquete.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServicePackageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        CreateServicePackageRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_createValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _packageService.CreateAsync(request, cancellationToken);

        return result.Success
            ? CreatedAtAction(
                nameof(GetById), new { id = result.Data!.Id }, ApiResponse.Ok(result.Data, Meta))
            : FromFailure(result);
    }

    /// <summary>
    /// Edición. La lista de `items` **reemplaza la composición entera**, igual
    /// que el horario semanal de Empleados. No cambia el estado de baja.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServicePackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id, UpdateServicePackageRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_updateValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _packageService.UpdateAsync(id, request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>Baja lógica, idempotente. El paquete deja de ofrecerse.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServicePackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _packageService.DeactivateAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>Deshace una baja. Idempotente.</summary>
    [HttpPost("{id:int}/reactivate")]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<ServicePackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(int id, CancellationToken cancellationToken)
    {
        var result = await _packageService.ReactivateAsync(id, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }
}
