using AutoMapper;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Mapping;

/// <summary>
/// Mapeos del módulo de Clientes. Solo entidad → DTO, mismo criterio que
/// EmployeeProfile: el alta y la edición se construyen a mano en el servicio.
/// </summary>
public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(
                dest => dest.FullName,
                opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()));

        CreateMap<Customer, CustomerDetailDto>()
            .IncludeBase<Customer, CustomerDto>();

        CreateMap<CustomerConsent, CustomerConsentDto>();
        CreateMap<CustomerAllergy, CustomerAllergyDto>();
        CreateMap<CustomerNote, CustomerNoteDto>();
    }
}
