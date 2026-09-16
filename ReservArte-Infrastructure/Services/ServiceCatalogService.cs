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

    // ── Categorías (RA-869f2wtrk) ─────────────────────────────────────────

    public async Task<Result<ServiceCategoryDto>> CreateCategoryAsync(
        CreateServiceCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is not { } organizationId)
        {
            return TenantNotResolved<ServiceCategoryDto>();
        }

        var category = new ServiceCategory
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Color = request.Color?.Trim(),
            DisplayOrder = request.DisplayOrder,
        };

        _repository.AddCategory(category);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ServiceCategoryDto>.Ok(_mapper.Map<ServiceCategoryDto>(category));
    }

    public async Task<Result<ServiceCategoryDto>> UpdateCategoryAsync(
        int categoryId, UpdateServiceCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _repository.GetCategoryByIdAsync(categoryId, cancellationToken);

        if (category is null)
        {
            return CategoryNotFound<ServiceCategoryDto>(categoryId);
        }

        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.Color = request.Color?.Trim();
        category.DisplayOrder = request.DisplayOrder;

        _repository.UpdateCategory(category);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ServiceCategoryDto>.Ok(_mapper.Map<ServiceCategoryDto>(category));
    }

    public Task<Result<ServiceCategoryDto>> DeactivateCategoryAsync(
        int categoryId, CancellationToken cancellationToken = default) =>
        SetCategoryActiveAsync(categoryId, isActive: false, cancellationToken);

    public Task<Result<ServiceCategoryDto>> ReactivateCategoryAsync(
        int categoryId, CancellationToken cancellationToken = default) =>
        SetCategoryActiveAsync(categoryId, isActive: true, cancellationToken);

    /// <summary>
    /// La baja de categoría se permite aunque tenga servicios: es lógica, así
    /// que ninguno queda sin clasificar y la categoría retirada sigue saliendo
    /// en GetCategoriesAsync sin filtro. La FK es Restrict y no llega a
    /// intervenir, porque no se borra ninguna fila.
    /// </summary>
    private async Task<Result<ServiceCategoryDto>> SetCategoryActiveAsync(
        int categoryId, bool isActive, CancellationToken cancellationToken)
    {
        var category = await _repository.GetCategoryByIdAsync(categoryId, cancellationToken);

        if (category is null)
        {
            return CategoryNotFound<ServiceCategoryDto>(categoryId);
        }

        if (category.IsActive != isActive)
        {
            category.IsActive = isActive;
            _repository.UpdateCategory(category);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Result<ServiceCategoryDto>.Ok(_mapper.Map<ServiceCategoryDto>(category));
    }

    // ── Variaciones (RA-869f2wtrk) ────────────────────────────────────────

    public async Task<Result<ServiceVariationDto>> AddVariationAsync(
        int serviceId, CreateServiceVariationRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(serviceId, cancellationToken);

        if (service is null)
        {
            return NotFound<ServiceVariationDto>(serviceId);
        }

        if (service.DurationMinutes + request.DurationModifier <= 0)
        {
            return InvalidDurationModifier<ServiceVariationDto>(service.DurationMinutes);
        }

        var variation = new ServiceVariation
        {
            OrganizationId = service.OrganizationId,
            ServiceId = service.Id,
            Name = request.Name.Trim(),
            PriceModifier = request.PriceModifier,
            DurationModifier = request.DurationModifier,
        };

        _repository.AddVariation(variation);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ServiceVariationDto>.Ok(_mapper.Map<ServiceVariationDto>(variation));
    }

    public async Task<Result<ServiceVariationDto>> UpdateVariationAsync(
        int serviceId, int variationId, UpdateServiceVariationRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await _repository.GetByIdAsync(serviceId, cancellationToken);

        if (service is null)
        {
            return NotFound<ServiceVariationDto>(serviceId);
        }

        var variation = await _repository.GetVariationAsync(serviceId, variationId, cancellationToken);

        if (variation is null)
        {
            return VariationNotFound<ServiceVariationDto>(variationId);
        }

        if (service.DurationMinutes + request.DurationModifier <= 0)
        {
            return InvalidDurationModifier<ServiceVariationDto>(service.DurationMinutes);
        }

        variation.Name = request.Name.Trim();
        variation.PriceModifier = request.PriceModifier;
        variation.DurationModifier = request.DurationModifier;

        _repository.UpdateVariation(variation);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ServiceVariationDto>.Ok(_mapper.Map<ServiceVariationDto>(variation));
    }

    /// <summary>
    /// Baja lógica, idempotente: GetVariationAsync devuelve también las
    /// retiradas, así que repetir la llamada no da 404.
    /// </summary>
    public async Task<Result<ServiceVariationDto>> DeleteVariationAsync(
        int serviceId, int variationId, CancellationToken cancellationToken = default)
    {
        var variation = await _repository.GetVariationAsync(serviceId, variationId, cancellationToken);

        if (variation is null)
        {
            return VariationNotFound<ServiceVariationDto>(variationId);
        }

        if (variation.IsActive)
        {
            variation.IsActive = false;
            _repository.UpdateVariation(variation);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Result<ServiceVariationDto>.Ok(_mapper.Map<ServiceVariationDto>(variation));
    }

    // ── Tarifas por nivel (RA-869f2wtrk) ──────────────────────────────────

    public async Task<Result<ServicePricingDto>> SetPricingAsync(
        int serviceId, string employeeLevel, UpsertServicePricingRequest request,
        CancellationToken cancellationToken = default)
    {
        var level = NormalizeLevel(employeeLevel);

        if (!EmployeeLevels.All.Contains(level))
        {
            return InvalidEmployeeLevel<ServicePricingDto>();
        }

        var service = await _repository.GetByIdAsync(serviceId, cancellationToken);

        if (service is null)
        {
            return NotFound<ServicePricingDto>(serviceId);
        }

        // Upsert: el índice único solo admite una tarifa vigente por servicio y
        // nivel, así que repetir el alta actualiza en vez de chocar.
        var pricing = await _repository.GetPricingAsync(serviceId, level, cancellationToken);

        if (pricing is null)
        {
            pricing = new ServicePricing
            {
                OrganizationId = service.OrganizationId,
                ServiceId = service.Id,
                EmployeeLevel = level,
                Price = request.Price,
            };

            _repository.AddPricing(pricing);
        }
        else
        {
            pricing.Price = request.Price;
            _repository.UpdatePricing(pricing);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ServicePricingDto>.Ok(_mapper.Map<ServicePricingDto>(pricing));
    }

    public async Task<Result<ServicePricingDto>> DeletePricingAsync(
        int serviceId, string employeeLevel, CancellationToken cancellationToken = default)
    {
        var level = NormalizeLevel(employeeLevel);

        if (!EmployeeLevels.All.Contains(level))
        {
            return InvalidEmployeeLevel<ServicePricingDto>();
        }

        var pricing = await _repository.GetPricingAsync(serviceId, level, cancellationToken);

        if (pricing is null)
        {
            return PricingNotFound<ServicePricingDto>(level);
        }

        pricing.IsActive = false;
        _repository.UpdatePricing(pricing);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ServicePricingDto>.Ok(_mapper.Map<ServicePricingDto>(pricing));
    }

    /// <summary>
    /// El nivel llega por la ruta: se normaliza para que `Senior` o ` senior `
    /// no se rechacen por un detalle de escritura. El catálogo es snake_case
    /// minúsculas.
    /// </summary>
    private static string NormalizeLevel(string employeeLevel) =>
        (employeeLevel ?? string.Empty).Trim().ToLowerInvariant();

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

    private static Result<T> CategoryNotFound<T>(int id) =>
        Result<T>.Fail(ErrorCodes.GenNotFound, $"No existe la categoría con id {id}.");

    /// <summary>
    /// La variación se busca siempre acotada a su servicio, así que una de otro
    /// servicio o de otro centro se comporta como inexistente.
    /// </summary>
    private static Result<T> VariationNotFound<T>(int id) =>
        Result<T>.Fail(ErrorCodes.GenNotFound, $"No existe la variación con id {id}.");

    private static Result<T> PricingNotFound<T>(string employeeLevel) =>
        Result<T>.Fail(
            ErrorCodes.GenNotFound,
            $"El servicio no tiene tarifa vigente para el nivel {employeeLevel}.");

    /// <summary>
    /// Se comprueba antes de tocar la base de datos: el CHECK del esquema lo
    /// rechazaría igual, pero como error de infraestructura y sin decir qué campo.
    /// </summary>
    private static Result<T> InvalidEmployeeLevel<T>() =>
        Result<T>.Fail(
            ErrorCodes.GenValidationFailed,
            $"El nivel debe ser uno de: {string.Join(", ", EmployeeLevels.All)}.",
            new List<ApiErrorDetail>
            {
                new()
                {
                    Field = "employeeLevel",
                    Code = ErrorCodes.GenValidationFailed,
                    Message = $"El nivel debe ser uno de: {string.Join(", ", EmployeeLevels.All)}.",
                },
            });

    /// <summary>
    /// El modificador puede ser negativo, pero no tanto como para dejar la
    /// duración resultante en cero: esa variante no ocuparía hueco en la agenda.
    /// </summary>
    private static Result<T> InvalidDurationModifier<T>(int serviceDuration) =>
        Result<T>.Fail(
            ErrorCodes.GenValidationFailed,
            $"La duración resultante debe ser mayor que 0 minutos (el servicio dura {serviceDuration}).",
            new List<ApiErrorDetail>
            {
                new()
                {
                    Field = "durationModifier",
                    Code = ErrorCodes.GenValidationFailed,
                    Message =
                        $"La duración resultante debe ser mayor que 0 minutos (el servicio dura {serviceDuration}).",
                },
            });
}
