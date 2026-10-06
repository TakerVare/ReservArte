using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Cuándo sale un recordatorio (RA-869d7f5zq). Cálculo puro: la cita va en hora
/// local del centro y el resultado es el instante en UTC.
/// </summary>
public static class ReminderSchedule
{
    /// <summary>
    /// Instante de envío: la hora de la cita menos la antelación del recordatorio.
    /// Si cae fuera de su franja de envío, se lleva al inicio de la franja de ese
    /// día (un aviso de madrugada sale por la mañana; uno a última hora, esa misma
    /// mañana, antes de lo previsto). Null si, tras el ajuste, el aviso saldría
    /// cuando la cita ya ha empezado.
    /// </summary>
    public static DateTime? SendAtUtc(
        DateOnly appointmentDate, TimeOnly startTime, ReminderConfiguration configuration, TimeZoneInfo timeZone)
    {
        var appointmentLocal = appointmentDate.ToDateTime(startTime);
        var sendLocal = appointmentLocal.AddHours(-configuration.HoursBeforeAppointment);

        if (configuration.AllowedSendStartTime is { } windowStart
            && configuration.AllowedSendEndTime is { } windowEnd)
        {
            var time = TimeOnly.FromDateTime(sendLocal);
            if (time < windowStart || time >= windowEnd)
            {
                sendLocal = DateOnly.FromDateTime(sendLocal).ToDateTime(windowStart);
            }
        }

        return sendLocal < appointmentLocal ? ToUtc(sendLocal, timeZone) : null;
    }

    /// <summary>
    /// Hora local del centro → UTC. Una hora que no existe (el salto del cambio
    /// al horario de verano) se toma una hora después, que es la que marca el reloj.
    /// </summary>
    public static DateTime ToUtc(DateTime local, TimeZoneInfo timeZone)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone);
    }
}
