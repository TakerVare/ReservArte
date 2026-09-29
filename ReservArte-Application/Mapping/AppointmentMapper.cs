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

    /// <summary>Cita de la agenda. Exige la clienta y la empleada cargadas.</summary>
    [MapperIgnoreSource(nameof(Appointment.OrganizationId))]
    [MapperIgnoreSource(nameof(Appointment.Organization))]
    [MapperIgnoreSource(nameof(Appointment.ServiceItems))]
    [MapperIgnoreSource(nameof(Appointment.RedsysOrderNumber))]
    [MapperIgnoreSource(nameof(Appointment.RedsysPreAuthToken))]
    [MapPropertyFromSource(nameof(AppointmentSummaryDto.CustomerName), Use = nameof(CustomerName))]
    [MapPropertyFromSource(nameof(AppointmentSummaryDto.EmployeeName), Use = nameof(EmployeeName))]
    public static partial AppointmentSummaryDto ToSummaryDto(Appointment source);

    /// <summary>Ficha de la cita. Exige clienta, empleada y líneas (con servicio y variación) cargadas.</summary>
    [MapperIgnoreSource(nameof(Appointment.OrganizationId))]
    [MapperIgnoreSource(nameof(Appointment.Organization))]
    [MapperIgnoreSource(nameof(Appointment.RedsysOrderNumber))]
    [MapperIgnoreSource(nameof(Appointment.RedsysPreAuthToken))]
    [MapPropertyFromSource(nameof(AppointmentDetailDto.CustomerName), Use = nameof(CustomerName))]
    [MapPropertyFromSource(nameof(AppointmentDetailDto.EmployeeName), Use = nameof(EmployeeName))]
    [MapProperty(nameof(Appointment.ServiceItems), nameof(AppointmentDetailDto.Items), Use = nameof(ToItemDtos))]
    public static partial AppointmentDetailDto ToDetailDto(Appointment source);

    [MapperIgnoreSource(nameof(AppointmentServiceItem.Id))]
    [MapperIgnoreSource(nameof(AppointmentServiceItem.OrganizationId))]
    [MapperIgnoreSource(nameof(AppointmentServiceItem.Organization))]
    [MapperIgnoreSource(nameof(AppointmentServiceItem.AppointmentId))]
    [MapperIgnoreSource(nameof(AppointmentServiceItem.Appointment))]
    [MapProperty([nameof(AppointmentServiceItem.Service), nameof(Service.Name)], nameof(AppointmentServiceItemDto.ServiceName))]
    [MapProperty([nameof(AppointmentServiceItem.ServiceVariation), nameof(ServiceVariation.Name)], nameof(AppointmentServiceItemDto.ServiceVariationName))]
    public static partial AppointmentServiceItemDto ToItemDto(AppointmentServiceItem source);

    private static IReadOnlyList<AppointmentServiceItemDto> ToItemDtos(ICollection<AppointmentServiceItem> items) =>
        items.OrderBy(i => i.Order).Select(ToItemDto).ToList();

    private static string CustomerName(Appointment source) =>
        $"{source.Customer.FirstName} {source.Customer.LastName}".Trim();

    private static string EmployeeName(Appointment source) =>
        $"{source.Employee.FirstName} {source.Employee.LastName}".Trim();
}
