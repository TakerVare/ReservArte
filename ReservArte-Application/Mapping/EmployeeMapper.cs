using ReservArte.Application.DTOs.Employees;
using ReservArte.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace ReservArte.Application.Mapping;

/// <summary>
/// Mapeos del módulo de Empleados. Solo entidad → DTO: la dirección contraria
/// se hace a mano en el servicio, porque el alta y la edición no son copias de
/// campos (el tenant y el rol se imponen en servidor, y el alta además crea la
/// cuenta de Identity asociada).
/// Mapperly genera el código al compilar: una propiedad del DTO sin origen (RMG012)
/// o de la entidad sin destino (RMG020) es un error de compilación (ver el csproj),
/// así que lo que no se expone se ignora a mano. Excepción: en ToDto(Employee) el
/// nombre completo se calcula desde la entidad entera, y con eso Mapperly deja de
/// comprobar sus miembros de origen (RMG012 sigue activo).
/// </summary>
[Mapper]
public static partial class EmployeeMapper
{
    [MapPropertyFromSource(nameof(EmployeeDto.FullName), Use = nameof(FullName))]
    public static partial EmployeeDto ToDto(Employee source);

    [MapperIgnoreSource(nameof(EmployeeAvailability.OrganizationId))]
    [MapperIgnoreSource(nameof(EmployeeAvailability.Organization))]
    [MapperIgnoreSource(nameof(EmployeeAvailability.EmployeeId))]
    [MapperIgnoreSource(nameof(EmployeeAvailability.Employee))]
    [MapperIgnoreSource(nameof(EmployeeAvailability.CreatedAt))]
    [MapperIgnoreSource(nameof(EmployeeAvailability.UpdatedAt))]
    public static partial EmployeeAvailabilityDto ToDto(EmployeeAvailability source);

    [MapperIgnoreSource(nameof(EmployeeException.OrganizationId))]
    [MapperIgnoreSource(nameof(EmployeeException.Organization))]
    [MapperIgnoreSource(nameof(EmployeeException.EmployeeId))]
    [MapperIgnoreSource(nameof(EmployeeException.Employee))]
    [MapperIgnoreSource(nameof(EmployeeException.CreatedAt))]
    [MapperIgnoreSource(nameof(EmployeeException.UpdatedAt))]
    public static partial EmployeeExceptionDto ToDto(EmployeeException source);

    /// <summary>Servicio que presta (4.1b): la asignación con el nombre y la duración del servicio.</summary>
    [MapProperty([nameof(EmployeeServiceAssignment.Service), nameof(Service.Name)], nameof(EmployeeServiceDto.Name))]
    [MapProperty([nameof(EmployeeServiceAssignment.Service), nameof(Service.DurationMinutes)], nameof(EmployeeServiceDto.DurationMinutes))]
    [MapProperty([nameof(EmployeeServiceAssignment.Service), nameof(Service.IsActive)], nameof(EmployeeServiceDto.ServiceIsActive))]
    [MapperIgnoreSource(nameof(EmployeeServiceAssignment.OrganizationId))]
    [MapperIgnoreSource(nameof(EmployeeServiceAssignment.Organization))]
    [MapperIgnoreSource(nameof(EmployeeServiceAssignment.EmployeeId))]
    [MapperIgnoreSource(nameof(EmployeeServiceAssignment.Employee))]
    [MapperIgnoreSource(nameof(EmployeeServiceAssignment.IsActive))]
    public static partial EmployeeServiceDto ToDto(EmployeeServiceAssignment source);

    private static string FullName(Employee source) => $"{source.FirstName} {source.LastName}".Trim();
}
