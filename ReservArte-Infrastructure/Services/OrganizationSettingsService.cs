using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Organizations;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Mapping;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.Infrastructure.Services;

/// <summary>Configuración del centro de la petición (RA-869f74u7y).</summary>
public class OrganizationSettingsService : IOrganizationSettingsService
{
    private readonly IOrganizationSettingsRepository _settings;
    private readonly ICurrentOrganizationService _currentOrganization;

    public OrganizationSettingsService(
        IOrganizationSettingsRepository settings,
        ICurrentOrganizationService currentOrganization)
    {
        _settings = settings;
        _currentOrganization = currentOrganization;
    }

    public async Task<Result<OrganizationSettingsDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved();
        }

        // Sin fila, los valores por defecto de la entidad: son los mismos con
        // los que ya está funcionando el centro.
        var settings = await _settings.GetCurrentAsync(cancellationToken) ?? new OrganizationSettings();

        return Result<OrganizationSettingsDto>.Ok(OrganizationMapper.ToDto(settings));
    }

    public async Task<Result<OrganizationSettingsDto>> UpdateAsync(
        UpdateOrganizationSettingsRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved();
        }

        var settings = await _settings.GetCurrentForUpdateAsync(cancellationToken);
        var isNew = settings is null;
        settings ??= new OrganizationSettings();

        // El validador ya ha exigido los tres campos.
        settings.TimeZone = request.TimeZone!;
        settings.CancellationHoursThreshold = request.CancellationHoursThreshold!.Value;
        settings.MaxNoShowsBeforeBlock = request.MaxNoShowsBeforeBlock!.Value;

        if (isNew)
        {
            _settings.Add(settings);
        }
        else
        {
            _settings.Update(settings);
        }

        await _settings.SaveChangesAsync(cancellationToken);

        return Result<OrganizationSettingsDto>.Ok(OrganizationMapper.ToDto(settings));
    }

    private static Result<OrganizationSettingsDto> TenantNotResolved() =>
        Result<OrganizationSettingsDto>.Fail(
            ErrorCodes.OrgTenantNotResolved,
            "No se ha podido resolver la organización de la petición.");
}
