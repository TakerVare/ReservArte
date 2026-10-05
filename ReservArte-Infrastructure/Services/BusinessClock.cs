using Microsoft.Extensions.Logging;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Zona horaria del centro, leída de su configuración (RA-869f74u7y). Sustituye
/// al `Europe/Madrid` fijo que tenían la disponibilidad y la reserva.
///
/// Es scoped y recuerda la zona mientras no cambie el centro: una misma petición
/// la pide varias veces (el día, las ausencias, el «ahora») y basta una consulta.
/// </summary>
public sealed class BusinessClock : IBusinessClock
{
    private readonly IOrganizationSettingsRepository _settings;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly ILogger<BusinessClock> _logger;

    private (Guid? OrganizationId, TimeZoneInfo? TimeZone)? _resolved;

    public BusinessClock(
        IOrganizationSettingsRepository settings,
        ICurrentOrganizationService currentOrganization,
        ILogger<BusinessClock> logger)
    {
        _settings = settings;
        _currentOrganization = currentOrganization;
        _logger = logger;
    }

    public async Task<TimeZoneInfo?> FindTimeZoneAsync(CancellationToken cancellationToken = default)
    {
        var organizationId = _currentOrganization.OrganizationId;

        // Un job puede cambiar de centro dentro del mismo scope: la zona
        // recordada solo vale para el centro con el que se resolvió.
        if (_resolved is { } resolved && resolved.OrganizationId == organizationId)
        {
            return resolved.TimeZone;
        }

        var settings = await _settings.GetCurrentAsync(cancellationToken);
        var timeZoneId = settings?.TimeZone ?? OrganizationSettings.DefaultTimeZone;

        // La API solo guarda zonas que resuelve, así que aquí solo se llega con
        // una máquina sin base de datos de zonas o con una fila escrita a mano.
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
        {
            _logger.LogWarning(
                "No se pudo resolver la zona horaria {TimeZoneId} de la organización {OrganizationId}",
                timeZoneId,
                organizationId);
        }

        _resolved = (organizationId, timeZone);

        return timeZone;
    }

    /// <summary>«Ahora» en la hora local de esa zona.</summary>
    public static DateTime Now(TimeProvider timeProvider, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime;
}
