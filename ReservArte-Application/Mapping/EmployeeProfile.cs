using AutoMapper;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Mapping;

/// <summary>
/// Mapeos del módulo de Empleados. Solo entidad → DTO: la dirección contraria
/// se hace a mano en el servicio, porque el alta y la edición no son copias de
/// campos (el tenant y el rol se imponen en servidor, y el alta además crea la
/// cuenta de Identity asociada).
/// </summary>
public class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        CreateMap<Employee, EmployeeDto>()
            .ForMember(
                dest => dest.FullName,
                opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()));

        CreateMap<EmployeeAvailability, EmployeeAvailabilityDto>();
    }
}
