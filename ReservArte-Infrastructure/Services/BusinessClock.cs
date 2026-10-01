using Microsoft.Extensions.Logging;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// «Hoy» y «ahora» en la hora local del centro (RA-869fagpx9), para la ventana de
/// reserva. Misma zona fija que <see cref="AvailabilityService.BusinessTimeZoneId"/>
/// y la misma deuda: llegará por centro con la configuración de la organización.
/// Sin la zona en la máquina se trabaja en UTC.
/// </summary>
internal static class BusinessClock
{
    public static TimeZoneInfo TimeZone(ILogger logger)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(AvailabilityService.BusinessTimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogWarning(ex, "No se pudo resolver la zona horaria {TimeZoneId}; se usa UTC",
                AvailabilityService.BusinessTimeZoneId);
            return TimeZoneInfo.Utc;
        }
    }

    public static DateTime Now(TimeProvider timeProvider, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime;
}
