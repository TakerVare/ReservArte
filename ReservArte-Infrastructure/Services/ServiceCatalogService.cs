using AutoMapper;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Services;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Casos de uso del catálogo de servicios (RA-869d7f3z0).
///
/// A diferencia de Empleados y Clientes, aquí no hay cuenta de Identity de por
/// medio: el catálogo es una tabla de negocio del centro. Por eso no necesita
/// `IUnitOfWork`, que existe para las escrituras que abarcan ficha y cuenta
/// (RA-869f1811u); todo lo de aquí cabe en un `SaveChanges`.
/// </summary>
public class ServiceCatalogService : IServiceCatalogService
{
    private readonly IServiceRepository _repository;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly IMapper _mapper;

    public ServiceCatalogService(
        IServiceRepository repository,
        ICurrentOrganizationService currentOrganization,
        IMapper mapper)
    {
        _repository = repository;
        _currentOrganization = currentOrganization;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<ServiceDto>>> GetPagedAsync(
        ServiceFilter filter, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is null)
        {
            return TenantNotResolved<PagedResult<ServiceDto>>();
        }

        var page = await _repository.GetPagedAsync(filter, cancellationToken);

        return Result<PagedResult<ServiceDto>>.Ok(new PagedResult<ServiceDto>
        {
            Items = page.Items.Select(_mapper.Map<ServiceDto>).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize,
        });
    }

    public async Task<Result<ServiceDetailDto>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetDetailAsync(id, cancellationToken);

        return service is null
            ? NotFound<ServiceDetailDto>(id)
            : Result<ServiceDetailDto>.Ok(_mapper.Map<ServiceDetailDto>(service));
    }

    public async Task<Result<ServiceDetailDto>> CreateAsync(
        CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is not { } organizationId)
        {
            return TenantNotResolved<ServiceDetailDto>();
        }

        // La categoría tiene que ser de ESTE centro: el repositorio ya acota por
        // tenant, así que una de otra organización se comporta como inexistente.
        if (request.CategoryId is { } categoryId
            && await _repository.GetCategoryByIdAsync(categoryId, cancellationToken) is null)
        {
            return UnknownCategory<ServiceDetailDto>();
        }

        var service = new Service
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DurationMinutes = request.DurationMinutes,
            BasePrice = request.BasePrice,
            CategoryId = request.CategoryId,
            ImageUrl = request.ImageUrl?.Trim(),
            RequiresAllergyTest = request.RequiresAllergyTest,
            AllergyTestHoursBefore = request.AllergyTestHoursBefore,
        };

        _repository.Add(service);
        await _repository.SaveChangesAsync(cancellationToken);

        // Se relee para devolver la categoría resuelta y las colecciones vacías
        // igual que las vería un GET posterior.
        var created = await _repository.GetDetailAsync(service.Id, cancellationToken);

        return Result<ServiceDetailDto>.Ok(_mapper.Map<ServiceDetailDto>(created ?? service));
    }

    public async Task<Result<ServiceDto>> UpdateAsync(
        int id, UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken);

        if (service is null)
        {
            return NotFound<ServiceDto>(id);
        }

        if (request.CategoryId is { } categoryId
            && await _repository.GetCategoryByIdAsync(categoryId, cancellationToken) is null)
        {
            return UnknownCategory<ServiceDto>();
        }

        service.Name = request.Name.Trim();
        service.Description = request.Description?.Trim();
        service.DurationMinutes = request.DurationMinutes;
        service.BasePrice = request.BasePrice;
        service.CategoryId = request.CategoryId;
        service.ImageUrl = request.ImageUrl?.Trim();
        service.RequiresAllergyTest = request.RequiresAllergyTest;
        service.AllergyTestHoursBefore = request.AllergyTestHoursBefore;

        _repository.Update(service);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ServiceDto>.Ok(_mapper.Map<ServiceDto>(service));
    }

    public Task<Result<ServiceDto>> DeactivateAsync(
        int id, CancellationToken cancellationToken = default) =>
        SetActiveAsync(id, isActive: false, cancellationToken);

    public Task<Result<ServiceDto>> ReactivateAsync(
        int id, CancellationToken cancellationToken = default) =>
        SetActiveAsync(id, isActive: true, cancellationToken);

    /// <summary>
    /// Baja y reactivación son la misma operación con distinto valor.
    /// Idempotente: si ya está en ese estado no se escribe nada, para no sellar
    /// UpdatedAt sin cambio real (mismo criterio que Clientes y Empleados).
    /// </summary>
    private async Task<Result<ServiceDto>> SetActiveAsync(
        int id, bool isActive, CancellationToken cancellationToken)
    {
        var service = await _repository.GetByIdAsync(id, cancellationToken);

        if (service is null)
        {
            return NotFound<ServiceDto>(id);
        }

        if (service.IsActive != isActive)
        {
            service.IsActive = isActive;
            _repository.Update(service);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Result<ServiceDto>.Ok(_mapper.Map<ServiceDto>(service));
    }

    public async Task<Result<IReadOnlyList<ServiceCategoryDto>>> GetCategoriesAsync(
        bool? isActive = null, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is null)
        {
            return TenantNotResolved<IReadOnlyList<ServiceCategoryDto>>();
        }

        var categories = await _repository.GetCategoriesAsync(isActive, cancellationToken);

        return Result<IReadOnlyList<ServiceCategoryDto>>.Ok(
            categories.Select(_mapper.Map<ServiceCategoryDto>).ToList());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Result<T> TenantNotResolved<T>() =>
        Result<T>.Fail(
            ErrorCodes.OrgTenantNotResolved,
            "No se ha podido resolver la organización de la petición.");

    /// <summary>
    /// Mismo resultado para «no existe» y «es de otra organización»:
    /// distinguirlos revelaría qué ids existen en otros centros.
    /// </summary>
    private static Result<T> NotFound<T>(int id) =>
        Result<T>.Fail(ErrorCodes.GenNotFound, $"No existe el servicio con id {id}.");

    private static Result<T> UnknownCategory<T>() =>
        Result<T>.Fail(
            ErrorCodes.GenValidationFailed,
            "La categoría indicada no existe en este centro.",
            new List<ApiErrorDetail>
            {
                new()
                {
                    Field = "categoryId",
                    Code = ErrorCodes.GenValidationFailed,
                    Message = "La categoría indicada no existe en este centro.",
                },
            });
}
