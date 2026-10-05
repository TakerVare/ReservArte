using ReservArte.Application.DTOs.Organizations;
using ReservArte.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace ReservArte.Application.Mapping;

/// <summary>Mapeos de la configuración del centro (RA-869f74u7y). Solo entidad → DTO.</summary>
[Mapper]
public static partial class OrganizationMapper
{
    [MapperIgnoreSource(nameof(OrganizationSettings.Id))]
    [MapperIgnoreSource(nameof(OrganizationSettings.OrganizationId))]
    [MapperIgnoreSource(nameof(OrganizationSettings.Organization))]
    [MapperIgnoreSource(nameof(OrganizationSettings.CreatedAt))]
    public static partial OrganizationSettingsDto ToDto(OrganizationSettings source);
}
