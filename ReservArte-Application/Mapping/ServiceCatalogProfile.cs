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
    }
}
