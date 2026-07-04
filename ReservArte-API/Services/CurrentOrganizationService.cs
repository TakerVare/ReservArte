using ReservArte.Domain.Interfaces;

namespace ReservArte.API.Services;

/// <summary>
/// Holder scoped del tenant actual: TenantMiddleware lo rellena una vez
/// por petición y el resto de capas solo leen.
/// </summary>
public class CurrentOrganizationService : ICurrentOrganizationService
{
    public Guid? OrganizationId { get; private set; }

    public bool IsResolved => OrganizationId.HasValue;

    public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;
}