using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Application.DTOs.Organizations;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

/// <summary>
/// Configuración del centro (RA-869f74u7y): zona horaria, umbral de cancelación
/// y máximo de no presentaciones. La lee cualquier rol autenticado (RA-869f6r71x):
/// la SPA necesita la zona para pintar ausencias y pruebas de alergia en hora del
/// centro, y no son datos personales. La escritura exige Admin o Manager.
/// </summary>
[ApiController]
[Route("api/v1/organization/settings")]
[Authorize]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
public class OrganizationSettingsController : ApiControllerBase
{
    /// <summary>Quién cambia la configuración.</summary>
    public const string ManagementRoles = Roles.Admin + "," + Roles.Manager;

    private readonly IOrganizationSettingsService _settingsService;
    private readonly IValidator<UpdateOrganizationSettingsRequest> _updateValidator;

    public OrganizationSettingsController(
        IOrganizationSettingsService settingsService,
        IValidator<UpdateOrganizationSettingsRequest> updateValidator)
    {
        _settingsService = settingsService;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Configuración vigente. Un centro que aún no la ha guardado recibe los
    /// valores por defecto (`Europe/Madrid`, 24 h y 3), con `updatedAt` nulo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<OrganizationSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _settingsService.GetAsync(cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }

    /// <summary>
    /// Reemplaza la configuración entera: los tres campos son obligatorios. La
    /// zona horaria es un identificador IANA; una que el servidor no reconozca es
    /// 400 en `timeZone` con código `UnknownTimeZone`.
    /// </summary>
    [HttpPut]
    [Authorize(Roles = ManagementRoles)]
    [ProducesResponseType(typeof(ApiResponse<OrganizationSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        UpdateOrganizationSettingsRequest request, CancellationToken cancellationToken)
    {
        var invalid = await ValidateAsync(_updateValidator, request, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _settingsService.UpdateAsync(request, cancellationToken);

        return result.Success ? Ok(ApiResponse.Ok(result.Data!, Meta)) : FromFailure(result);
    }
}
