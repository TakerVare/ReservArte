using ReservArte.Application.DTOs.Customers;
using ReservArte.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace ReservArte.Application.Mapping;

/// <summary>
/// Mapeos del módulo de Clientes. Solo entidad → DTO, mismo criterio que
/// EmployeeMapper: el alta y la edición se construyen a mano en el servicio.
/// Como allí, en los dos mapeos de la ficha el nombre completo se calcula desde la
/// entidad entera y Mapperly deja de comprobar sus miembros de origen (RMG012, el de
/// las propiedades del DTO, sigue activo).
/// </summary>
[Mapper]
public static partial class CustomerMapper
{
    [MapPropertyFromSource(nameof(CustomerDto.FullName), Use = nameof(FullName))]
    public static partial CustomerDto ToDto(Customer source);

    // El detalle repite la regla del nombre completo: cada método de Mapperly se
    // genera por separado, no hereda la configuración del de la clase base. Sus
    // colecciones usan los ToDto de abajo.
    [MapPropertyFromSource(nameof(CustomerDetailDto.FullName), Use = nameof(FullName))]
    public static partial CustomerDetailDto ToDetailDto(Customer source);

    // El consentimiento viaja sin Id: la ficha los identifica por su tipo.
    [MapperIgnoreSource(nameof(CustomerConsent.Id))]
    [MapperIgnoreSource(nameof(CustomerConsent.OrganizationId))]
    [MapperIgnoreSource(nameof(CustomerConsent.Organization))]
    [MapperIgnoreSource(nameof(CustomerConsent.CustomerId))]
    [MapperIgnoreSource(nameof(CustomerConsent.Customer))]
    [MapperIgnoreSource(nameof(CustomerConsent.IsActive))]
    [MapperIgnoreSource(nameof(CustomerConsent.CreatedAt))]
    [MapperIgnoreSource(nameof(CustomerConsent.UpdatedAt))]
    public static partial CustomerConsentDto ToDto(CustomerConsent source);

    [MapperIgnoreSource(nameof(CustomerAllergy.OrganizationId))]
    [MapperIgnoreSource(nameof(CustomerAllergy.Organization))]
    [MapperIgnoreSource(nameof(CustomerAllergy.CustomerId))]
    [MapperIgnoreSource(nameof(CustomerAllergy.Customer))]
    [MapperIgnoreSource(nameof(CustomerAllergy.IsActive))]
    [MapperIgnoreSource(nameof(CustomerAllergy.CreatedAt))]
    [MapperIgnoreSource(nameof(CustomerAllergy.UpdatedAt))]
    public static partial CustomerAllergyDto ToDto(CustomerAllergy source);

    [MapperIgnoreSource(nameof(CustomerNote.OrganizationId))]
    [MapperIgnoreSource(nameof(CustomerNote.Organization))]
    [MapperIgnoreSource(nameof(CustomerNote.CustomerId))]
    [MapperIgnoreSource(nameof(CustomerNote.Customer))]
    [MapperIgnoreSource(nameof(CustomerNote.Employee))]
    [MapperIgnoreSource(nameof(CustomerNote.IsActive))]
    [MapperIgnoreSource(nameof(CustomerNote.UpdatedAt))]
    public static partial CustomerNoteDto ToDto(CustomerNote source);

    private static string FullName(Customer source) => $"{source.FirstName} {source.LastName}".Trim();
}
