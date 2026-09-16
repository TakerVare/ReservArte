using AutoMapper;
using ReservArte.Application.DTOs.Services;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Mapping;

/// <summary>
/// Mapeos del catálogo de servicios. Solo entidad → DTO, mismo criterio que
/// EmployeeProfile y CustomerProfile: el alta y la edición se construyen a mano
/// en el servicio.
/// </summary>
public class ServiceCatalogProfile : Profile
{
    public ServiceCatalogProfile()
    {
        CreateMap<Service, ServiceDto>()
            // La categoría es opcional: sin ella el nombre viaja como null en
            // lugar de romper el mapeo.
            .ForMember(
                dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));

        CreateMap<Service, ServiceDetailDto>()
            .IncludeBase<Service, ServiceDto>();

        CreateMap<ServiceVariation, ServiceVariationDto>();
        CreateMap<ServicePricing, ServicePricingDto>();
        CreateMap<ServiceCategory, ServiceCategoryDto>();

        // Paquetes (RA-869d7f45n). La línea trae del servicio los datos que la
        // ficha necesita para mostrar el desglose sin una segunda llamada.
        CreateMap<ServicePackageItem, ServicePackageItemDto>()
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.Name))
            .ForMember(dest => dest.BasePrice, opt => opt.MapFrom(src => src.Service.BasePrice))
            .ForMember(
                dest => dest.DurationMinutes,
                opt => opt.MapFrom(src => src.Service.DurationMinutes));

        // ServicePackageDto NO se mapea aquí: itemsTotalPrice, savings y
        // totalDurationMinutes se calculan desde los servicios incluidos, así
        // que lo compone ServicePackageService.
    }
}
