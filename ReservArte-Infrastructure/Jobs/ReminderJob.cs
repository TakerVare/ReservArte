using Microsoft.Extensions.Logging;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Jobs;

/// <summary>
/// Job de un aviso de cita (RA-869d7f5zq). Corre sin petición HTTP, así que no
/// hay tenant resuelto: lo primero que hace es fijar el centro que viaja en sus
/// argumentos, y a partir de ahí todo se lee a través de los query filters, como
/// en una petición. No necesita el ámbito de sistema: nunca mira otro centro.
/// </summary>
public class ReminderJob
{
    private readonly IReminderService _reminders;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly ILogger<ReminderJob> _logger;

    public ReminderJob(
        IReminderService reminders,
        ICurrentOrganizationService currentOrganization,
        ILogger<ReminderJob> logger)
    {
        _reminders = reminders;
        _currentOrganization = currentOrganization;
        _logger = logger;
    }

    /// <summary>
    /// Argumentos sencillos a propósito: la cola los guarda serializados y un
    /// cambio en un tipo propio rompería los jobs ya programados.
    /// </summary>
    public async Task RunAsync(Guid organizationId, int appointmentId, int reminderConfigurationId, string channel)
    {
        _currentOrganization.SetOrganization(organizationId);

        var outcome = await _reminders.ProcessDueAsync(appointmentId, reminderConfigurationId, channel);

        _logger.LogInformation(
            "Job de recordatorio (centro {OrganizationId}, cita {AppointmentId}, recordatorio {ReminderConfigurationId}, {Channel}): {Outcome}",
            organizationId,
            appointmentId,
            reminderConfigurationId,
            channel,
            outcome.Value);
    }
}
