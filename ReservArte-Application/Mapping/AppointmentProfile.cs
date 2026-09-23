using AutoMapper;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Mapping;

/// <summary>
/// Mapeos del módulo de Citas. Solo entidad → DTO, mismo criterio que
/// EmployeeProfile y CustomerProfile: las transiciones tocan la entidad a mano
/// en el servicio, porque cada una cambia un juego distinto de campos.
/// </summary>
public class AppointmentProfile : Profile
{
    public AppointmentProfile()
    {
        CreateMap<Appointment, AppointmentDto>();
    }
}
