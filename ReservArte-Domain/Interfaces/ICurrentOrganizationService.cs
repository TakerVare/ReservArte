namespace ReservArte.Domain.Interfaces;

/// <summary>
/// Organización (tenant) del scope de la petición actual. La resuelve
/// TenantMiddleware (cabecera en dev, subdominio en prod — vol. 1 §5.1.3)
/// y la consumirán los query filters de EF Core y los servicios de
/// aplicación. La integración con el claim del JWT llegará con Auth.
/// </summary>
public interface ICurrentOrganizationService
{
    Guid? OrganizationId { get; }

    bool IsResolved { get; }

    void SetOrganization(Guid organizationId);
}