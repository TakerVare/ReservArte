using ReservArte.Domain.Interfaces;
using System.Security.Claims;

namespace ReservArte.API.Services;

public class HttpContextOrganizationService : ICurrentOrganizationService
{
    // Debe coincidir con el claim que se incluya en el JWT al autenticar.
    private const string OrgIdClaimType = "org_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextOrganizationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int OrganizationId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?
                .User.FindFirstValue(OrgIdClaimType);

            if (!int.TryParse(value, out var orgId))
                throw new UnauthorizedAccessException(
                    "El claim 'org_id' no está presente o no es un entero válido.");

            return orgId;
        }
    }
}
