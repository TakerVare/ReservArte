using ReservArte.Application.DTOs.Services;
using ReservArte.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace ReservArte.Application.Mapping;

/// <summary>
/// Mapeos del catálogo de servicios. Solo entidad → DTO, mismo criterio que
/// EmployeeMapper y CustomerMapper: el alta y la edición se construyen a mano
/// en el servicio. Lo que no se expone se ignora a mano (RMG020 es un error).
/// </summary>
[Mapper]
public static partial class ServiceCatalogMapper
{
    // La categoría es opcional: sin ella el nombre viaja como null en lugar de
    // romper el mapeo (Mapperly genera el acceso con comprobación de nulo).
    // El listado no lleva variaciones ni tarifas: son del detalle.
    [MapProperty([nameof(Service.Category), nameof(ServiceCategory.Name)], nameof(ServiceDto.CategoryName))]
    [MapperIgnoreSource(nameof(Service.OrganizationId))]
    [MapperIgnoreSource(nameof(Service.Organization))]
    [MapperIgnoreSource(nameof(Service.Employees))]
    [MapperIgnoreSource(nameof(Service.PackageItems))]
    [MapperIgnoreSource(nameof(Service.Variations))]
    [MapperIgnoreSource(nameof(Service.Pricings))]
    public static partial ServiceDto ToDto(Service source);

    [MapProperty([nameof(Service.Category), nameof(ServiceCategory.Name)], nameof(ServiceDetailDto.CategoryName))]
    [MapperIgnoreSource(nameof(Service.OrganizationId))]
    [MapperIgnoreSource(nameof(Service.Organization))]
    [MapperIgnoreSource(nameof(Service.Employees))]
    [MapperIgnoreSource(nameof(Service.PackageItems))]
    public static partial ServiceDetailDto ToDetailDto(Service source);

    [MapperIgnoreSource(nameof(ServiceVariation.OrganizationId))]
    [MapperIgnoreSource(nameof(ServiceVariation.Organization))]
    [MapperIgnoreSource(nameof(ServiceVariation.ServiceId))]
    [MapperIgnoreSource(nameof(ServiceVariation.Service))]
    [MapperIgnoreSource(nameof(ServiceVariation.IsActive))]
    [MapperIgnoreSource(nameof(ServiceVariation.CreatedAt))]
    [MapperIgnoreSource(nameof(ServiceVariation.UpdatedAt))]
    public static partial ServiceVariationDto ToDto(ServiceVariation source);

    [MapperIgnoreSource(nameof(ServicePricing.OrganizationId))]
    [MapperIgnoreSource(nameof(ServicePricing.Organization))]
    [MapperIgnoreSource(nameof(ServicePricing.ServiceId))]
    [MapperIgnoreSource(nameof(ServicePricing.Service))]
    [MapperIgnoreSource(nameof(ServicePricing.IsActive))]
    [MapperIgnoreSource(nameof(ServicePricing.CreatedAt))]
    [MapperIgnoreSource(nameof(ServicePricing.UpdatedAt))]
    public static partial ServicePricingDto ToDto(ServicePricing source);

    [MapperIgnoreSource(nameof(ServiceCategory.OrganizationId))]
    [MapperIgnoreSource(nameof(ServiceCategory.Organization))]
    [MapperIgnoreSource(nameof(ServiceCategory.Services))]
    [MapperIgnoreSource(nameof(ServiceCategory.CreatedAt))]
    [MapperIgnoreSource(nameof(ServiceCategory.UpdatedAt))]
    public static partial ServiceCategoryDto ToDto(ServiceCategory source);

    // Paquetes (RA-869d7f45n). La línea trae del servicio los datos que la ficha
    // necesita para mostrar el desglose sin una segunda llamada.
    // ServicePackageDto NO se mapea aquí: itemsTotalPrice, savings y
    // totalDurationMinutes se calculan desde los servicios incluidos, así que lo
    // compone ServicePackageService.
    [MapProperty([nameof(ServicePackageItem.Service), nameof(Service.Name)], nameof(ServicePackageItemDto.ServiceName))]
    [MapProperty([nameof(ServicePackageItem.Service), nameof(Service.BasePrice)], nameof(ServicePackageItemDto.BasePrice))]
    [MapProperty(
        [nameof(ServicePackageItem.Service), nameof(Service.DurationMinutes)],
        nameof(ServicePackageItemDto.DurationMinutes))]
    [MapperIgnoreSource(nameof(ServicePackageItem.OrganizationId))]
    [MapperIgnoreSource(nameof(ServicePackageItem.Organization))]
    [MapperIgnoreSource(nameof(ServicePackageItem.ServicePackageId))]
    [MapperIgnoreSource(nameof(ServicePackageItem.ServicePackage))]
    [MapperIgnoreSource(nameof(ServicePackageItem.IsActive))]
    public static partial ServicePackageItemDto ToDto(ServicePackageItem source);
}
