using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

[ApiController]
[Route("api/v1/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private ApiMeta Meta => ApiMeta.Create(HttpContext.TraceIdentifier);

    /// <summary>
    /// Datos del usuario autenticado, leídos de los claims del JWT. Primer
    /// endpoint protegido del proyecto: valida de punta a punta el esquema
    /// JwtBearer. Los endpoints MFA se añaden en las siguientes fases.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var data = new
        {
            id = User.FindFirstValue("sub"),
            email = User.FindFirstValue("email"),
            role = User.FindFirstValue("role"),
            organizationId = User.FindFirstValue("organization_id"),
        };

        return Ok(ApiResponse.Ok<object>(data, Meta));
    }
}