using System.Security.Claims;
using ReservArte.Domain.Interfaces;

namespace ReservArte.API.Services;

/// <summary>
/// Lee el usuario de la petición de los claims ya validados por JwtBearer.
/// Los nombres son los cortos que emite JwtTokenService (`sub`, `role`):
/// `MapInboundClaims = false` evita que se traduzcan a las URIs de ClaimTypes.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(Principal?.FindFirstValue("sub"), out var id) ? id : null;

    public string? Role => Principal?.FindFirstValue("role");
}
