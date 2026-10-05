using Microsoft.Extensions.Logging;
using ReservArte.Application.Interfaces;

namespace ReservArte.Infrastructure.Jobs;

/// <summary>
/// Cola de avisos apagada (<c>Hangfire:Storage:Provider = None</c>, solo en
/// Development): los avisos quedan registrados como pendientes, pero nadie los
/// dispara. Lo dice en el log cada vez, para que no pase por un fallo silencioso.
/// </summary>
public class DisabledReminderJobScheduler : IReminderJobScheduler
{
    private readonly ILogger<DisabledReminderJobScheduler> _logger;

    public DisabledReminderJobScheduler(ILogger<DisabledReminderJobScheduler> logger) => _logger = logger;

    public void Schedule(ReminderJobKey key, DateTimeOffset sendAt) =>
        _logger.LogWarning(
            "Cola de trabajos apagada: el recordatorio de la cita {AppointmentId} para {SendAt:u} no se programará",
            key.AppointmentId,
            sendAt);
}
