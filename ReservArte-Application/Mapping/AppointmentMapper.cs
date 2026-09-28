using ReservArte.Application.DTOs.Appointments;
using ReservArte.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace ReservArte.Application.Mapping;

/// <summary>
/// Mapeos del módulo de Citas. Solo entidad → DTO, mismo criterio que
/// EmployeeMapper y CustomerMapper: las transiciones tocan la entidad a mano
/// en el servicio, porque cada una cambia un juego distinto de campos.
/// Lo que no se expone se ignora a mano: el tenant, las navegaciones y los datos
/// de Redsys, que no salen nunca hacia el cliente.
/// </summary>
[Mapper]
public static partial class AppointmentMapper
{
    [MapperIgnoreSource(nameof(Appointment.OrganizationId))]
    [MapperIgnoreSource(nameof(Appointment.Organization))]
    [MapperIgnoreSource(nameof(Appointment.Customer))]
    [MapperIgnoreSource(nameof(Appointment.Employee))]
    [MapperIgnoreSource(nameof(Appointment.ServiceItems))]
    [MapperIgnoreSource(nameof(Appointment.RedsysOrderNumber))]
    [MapperIgnoreSource(nameof(Appointment.RedsysPreAuthToken))]
    public static partial AppointmentDto ToDto(Appointment source);
}
