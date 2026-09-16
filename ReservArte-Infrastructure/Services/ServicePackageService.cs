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
/// Casos de uso de los paquetes del catálogo (RA-869d7f45n).
///
/// Como el resto del catálogo, no hay cuenta de Identity de por medio, así que
/// no necesita `IUnitOfWork`: el paquete y su composición se guardan con un
/// único `SaveChanges`.
/// </summary>
public class ServicePackageService : IServicePackageService
{
    private readonly IServicePackageRepository _repository;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly IMapper _mapper;

    public ServicePackageService(
        IServicePackageRepository repository,
        ICurrentOrganizationService currentOrganization,
        IMapper mapper)
    {
        _repository = repository;
        _currentOrganization = currentOrganization;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<ServicePackageDto>>> GetPagedAsync(
        ServicePackageFilter filter, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is null)
        {
            return TenantNotResolved<PagedResult<ServicePackageDto>>();
        }

        var page = await _repository.GetPagedAsync(filter, cancellationToken);

        return Result<PagedResult<ServicePackageDto>>.Ok(new PagedResult<ServicePackageDto>
        {
            Items = page.Items.Select(ToDto).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize,
        });
    }

    public async Task<Result<ServicePackageDto>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var package = await _repository.GetDetailAsync(id, cancellationToken);

        return package is null ? NotFound<ServicePackageDto>(id) : Result<ServicePackageDto>.Ok(ToDto(package));
    }

    public async Task<Result<ServicePackageDto>> CreateAsync(
        CreateServicePackageRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is not { } organizationId)
        {
            return TenantNotResolved<ServicePackageDto>();
        }

        var unknown = await UnknownServicesAsync(request.Items, cancellationToken);
        if (unknown is not null)
        {
            return unknown.As<ServicePackageDto>();
        }

        var package = new ServicePackage
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            TotalPrice = request.TotalPrice,
            DiscountPercentage = request.DiscountPercentage,
        };

        _repository.Add(package);
        await _repository.SaveChangesAsync(cancellationToken);

        // Las líneas se añaden después del primer SaveChanges porque necesitan
        // el Id del paquete; el repositorio les impone paquete y tenant.
        await _repository.ReplaceItemsAsync(
            package.Id, BuildItems(request.Items, organizationId), cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var created = await _repository.GetDetailAsync(package.Id, cancellationToken);

        return Result<ServicePackageDto>.Ok(ToDto(created ?? package));
    }

    public async Task<Result<ServicePackageDto>> UpdateAsync(
        int id, UpdateServicePackageRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is not { } organizationId)
        {
            return TenantNotResolved<ServicePackageDto>();
        }

        var package = await _repository.GetByIdAsync(id, cancellationToken);

        if (package is null)
        {
            return NotFound<ServicePackageDto>(id);
        }

        var unknown = await UnknownServicesAsync(request.Items, cancellationToken);
        if (unknown is not null)
        {
            return unknown.As<ServicePackageDto>();
        }

        package.Name = request.Name.Trim();
        package.Description = request.Description?.Trim();
        package.ImageUrl = request.ImageUrl?.Trim();
        package.TotalPrice = request.TotalPrice;
        package.DiscountPercentage = request.DiscountPercentage;

        _repository.Update(package);

        // El PUT reemplaza la composición entera, como el horario semanal de
        // Empleados: es la forma natural de editarla en un formulario.
        await _repository.ReplaceItemsAsync(
            package.Id, BuildItems(request.Items, organizationId), cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var updated = await _repository.GetDetailAsync(package.Id, cancellationToken);

        return Result<ServicePackageDto>.Ok(ToDto(updated ?? package));
    }

    public Task<Result<ServicePackageDto>> DeactivateAsync(
        int id, CancellationToken cancellationToken = default) =>
        SetActiveAsync(id, isActive: false, cancellationToken);

    public Task<Result<ServicePackageDto>> ReactivateAsync(
        int id, CancellationToken cancellationToken = default) =>
        SetActiveAsync(id, isActive: true, cancellationToken);

    /// <summary>
    /// Baja y reactivación son la misma operación con distinto valor.
    /// Idempotente: si ya está en ese estado no se escribe nada, para no sellar
    /// UpdatedAt sin cambio real.
    /// </summary>
    private async Task<Result<ServicePackageDto>> SetActiveAsync(
        int id, bool isActive, CancellationToken cancellationToken)
    {
        var package = await _repository.GetByIdAsync(id, cancellationToken);

        if (package is null)
        {
            return NotFound<ServicePackageDto>(id);
        }

        if (package.IsActive != isActive)
        {
            package.IsActive = isActive;
            _repository.Update(package);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        var detail = await _repository.GetDetailAsync(id, cancellationToken);

        return Result<ServicePackageDto>.Ok(ToDto(detail ?? package));
    }

    /// <summary>
    /// Comprueba en bloque que todos los servicios de la composición existen en
    /// este centro. El repositorio ya acota por tenant, así que uno de otra
    /// organización se comporta como inexistente.
    /// </summary>
    private async Task<Result<object>?> UnknownServicesAsync(
        IReadOnlyList<ServicePackageItemRequest> items, CancellationToken cancellationToken)
    {
        var existing = await _repository.ExistingServiceIdsAsync(
            items.Select(i => i.ServiceId), cancellationToken);

        var details = items
            .Select((item, index) => (item, index))
            .Where(x => !existing.Contains(x.item.ServiceId))
            .Select(x => new ApiErrorDetail
            {
                // Índice de la línea: el formulario necesita saber CUÁL falla.
                Field = $"items[{x.index}].serviceId",
                Code = ErrorCodes.GenValidationFailed,
                Message = $"El servicio con id {x.item.ServiceId} no existe en este centro.",
            })
            .ToList();

        return details.Count == 0
            ? null
            : Result<object>.Fail(
                ErrorCodes.GenValidationFailed,
                "Algún servicio del paquete no existe en este centro.",
                details);
    }

    private static List<ServicePackageItem> BuildItems(
        IReadOnlyList<ServicePackageItemRequest> items, Guid organizationId) =>
        items
            .Select(i => new ServicePackageItem
            {
                OrganizationId = organizationId,
                ServiceId = i.ServiceId,
                Order = i.Order,
            })
            .ToList();

    /// <summary>
    /// Compone el DTO a mano y no con AutoMapper: `itemsTotalPrice`, `savings` y
    /// `totalDurationMinutes` se calculan desde los servicios incluidos, no se
    /// guardan. Así, si cambia el precio de un servicio, el desglose cambia solo
    /// mientras `TotalPrice` sigue siendo el pactado.
    /// </summary>
    private ServicePackageDto ToDto(ServicePackage package)
    {
        var items = package.Items
            .OrderBy(i => i.Order)
            .Select(_mapper.Map<ServicePackageItemDto>)
            .ToList();

        var itemsTotal = items.Sum(i => i.BasePrice);

        return new ServicePackageDto
        {
            Id = package.Id,
            Name = package.Name,
            Description = package.Description,
            ImageUrl = package.ImageUrl,
            TotalPrice = package.TotalPrice,
            DiscountPercentage = package.DiscountPercentage,
            ItemsTotalPrice = itemsTotal,
            Savings = itemsTotal - package.TotalPrice,
            TotalDurationMinutes = items.Sum(i => i.DurationMinutes),
            IsActive = package.IsActive,
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt,
            Items = items,
        };
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
        Result<T>.Fail(ErrorCodes.GenNotFound, $"No existe el paquete con id {id}.");
}

/// <summary>
/// Reetiqueta un fallo para otro tipo de dato. Evita repetir la construcción del
/// error cuando la comprobación es común a varias operaciones.
/// </summary>
internal static class ResultFailureExtensions
{
    public static Result<T> As<T>(this Result<object> failure) =>
        Result<T>.Fail(failure.ErrorCode!, failure.ErrorMessage!, failure.ErrorDetails);
}
