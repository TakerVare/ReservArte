using Microsoft.Extensions.Logging;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Programación de los recordatorios de cita (RA-869d7f5zq).
///
/// Cada aviso es un <see cref="ReminderLog"/> que nace <c>pending</c> al
/// programarse, uno por cita, recordatorio y canal (H-49), más un job en la cola
/// para su hora. El job no confía en lo que se sabía al programarlo: cuando le
/// llega la hora vuelve a mirar la cita. Por eso mover una cita solo exige
/// programar de nuevo: el job antiguo se descarta solo si la cita se fue a más
/// tarde, y si se adelantó, el nuevo llega antes y el antiguo encuentra el aviso
/// ya tratado.
/// </summary>
public class ReminderService : IReminderService
{
    /// <summary>
    /// Margen al comparar la hora prevista con el reloj: la cola dispara con
    /// algunos segundos de diferencia.
    /// </summary>
    private static readonly TimeSpan DueTolerance = TimeSpan.FromMinutes(1);

    private readonly IReminderRepository _reminders;
    private readonly IAppointmentRepository _appointments;
    private readonly IReminderJobScheduler _jobs;
    private readonly IBusinessClock _businessClock;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        IReminderRepository reminders,
        IAppointmentRepository appointments,
        IReminderJobScheduler jobs,
        IBusinessClock businessClock,
        ICurrentOrganizationService currentOrganization,
        TimeProvider timeProvider,
        ILogger<ReminderService> logger)
    {
        _reminders = reminders;
        _appointments = appointments;
        _jobs = jobs;
        _businessClock = businessClock;
        _currentOrganization = currentOrganization;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task ScheduleForAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await ScheduleAsync(appointmentId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // La cita ya está confirmada y guardada: quedarse sin aviso es peor
            // que devolver un error por algo que la persona no puede arreglar.
            _logger.LogError(ex, "No se pudieron programar los recordatorios de la cita {AppointmentId}", appointmentId);
        }
    }

    private async Task ScheduleAsync(int appointmentId, CancellationToken cancellationToken)
    {
        if (_currentOrganization.OrganizationId is not { } organizationId)
        {
            return;
        }

        var appointment = await _appointments.GetByIdAsync(appointmentId, cancellationToken);
        if (appointment is not { IsActive: true, Status: AppointmentStatuses.Confirmed })
        {
            return;
        }

        var configurations = await _reminders.GetActiveConfigurationsAsync(cancellationToken);
        if (configurations.Count == 0)
        {
            return;
        }

        var timeZone = await _businessClock.FindTimeZoneAsync(cancellationToken) ?? TimeZoneInfo.Utc;
        var now = _timeProvider.GetUtcNow();
        var logs = await _reminders.GetLogsForAppointmentAsync(appointmentId, cancellationToken);
        var scheduled = new List<(ReminderJobKey Key, DateTimeOffset SendAt)>();

        foreach (var configuration in configurations)
        {
            var sendAtUtc = ReminderSchedule.SendAtUtc(
                appointment.AppointmentDate, appointment.StartTime, configuration, timeZone);

            // Sin hora válida o con la hora ya pasada (cita confirmada con poca
            // antelación), ese aviso no se manda.
            if (sendAtUtc is not { } sendAt || sendAt <= now.UtcDateTime)
            {
                continue;
            }

            foreach (var channel in ChannelsOf(configuration))
            {
                var log = logs.FirstOrDefault(
                    l => l.ReminderConfigurationId == configuration.Id && l.Channel == channel);

                // Enviado o fallido: no se repite aunque la cita se mueva.
                if (log is not null && log.Status != ReminderLogStatuses.Pending)
                {
                    continue;
                }

                if (log is null)
                {
                    _reminders.AddLog(new ReminderLog
                    {
                        AppointmentId = appointment.Id,
                        ReminderConfigurationId = configuration.Id,
                        Channel = channel,
                    });
                }

                scheduled.Add((
                    new ReminderJobKey(organizationId, appointment.Id, configuration.Id, channel),
                    new DateTimeOffset(sendAt, TimeSpan.Zero)));
            }
        }

        // Primero los registros y después la cola: un job sin registro lo
        // crearía al llegar su hora, pero un registro que no se guarda dejaría
        // el aviso sin rastro.
        await _reminders.SaveChangesAsync(cancellationToken);

        foreach (var (key, sendAt) in scheduled)
        {
            _jobs.Schedule(key, sendAt);
        }

        _logger.LogInformation(
            "Cita {AppointmentId}: {Count} recordatorio(s) programado(s)", appointment.Id, scheduled.Count);
    }

    public async Task<ReminderDueOutcome> ProcessDueAsync(
        int appointmentId, int reminderConfigurationId, string channel, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointments.GetByIdAsync(appointmentId, cancellationToken);
        var configuration = await _reminders.GetConfigurationAsync(reminderConfigurationId, cancellationToken);
        var log = await _reminders.GetLogAsync(appointmentId, reminderConfigurationId, channel, cancellationToken);

        if (log is not null && log.Status != ReminderLogStatuses.Pending)
        {
            return new ReminderDueOutcome(ReminderDueOutcomes.AlreadyHandled, log.Id);
        }

        // Cita cancelada, cerrada, retirada o de otro centro; recordatorio
        // desactivado o que ya no usa este canal: el aviso pendiente sobra.
        if (appointment is not { IsActive: true, Status: AppointmentStatuses.Confirmed }
            || configuration is not { IsActive: true }
            || !ChannelsOf(configuration).Contains(channel))
        {
            if (log is not null)
            {
                _reminders.RemoveLog(log);
                await _reminders.SaveChangesAsync(cancellationToken);
            }

            return new ReminderDueOutcome(ReminderDueOutcomes.Discarded);
        }

        var timeZone = await _businessClock.FindTimeZoneAsync(cancellationToken) ?? TimeZoneInfo.Utc;
        var sendAtUtc = ReminderSchedule.SendAtUtc(
            appointment.AppointmentDate, appointment.StartTime, configuration, timeZone);

        // La cita se movió a más tarde después de programar este job: hay otro
        // en la cola para la hora nueva.
        if (sendAtUtc is { } sendAt && sendAt - DueTolerance > _timeProvider.GetUtcNow().UtcDateTime)
        {
            return new ReminderDueOutcome(ReminderDueOutcomes.NotYet, log?.Id);
        }

        if (log is null)
        {
            log = new ReminderLog
            {
                AppointmentId = appointmentId,
                ReminderConfigurationId = reminderConfigurationId,
                Channel = channel,
            };
            _reminders.AddLog(log);
            await _reminders.SaveChangesAsync(cancellationToken);
        }

        // El envío por canal llega con RA-869d7f61y: hasta entonces el aviso se
        // queda pendiente, que es exactamente lo que es.
        _logger.LogInformation(
            "Recordatorio {ReminderLogId} de la cita {AppointmentId} listo para enviar por {Channel}",
            log.Id,
            appointmentId,
            channel);

        return new ReminderDueOutcome(ReminderDueOutcomes.Ready, log.Id);
    }

    /// <summary>Un envío por canal: <c>both</c> son dos (H-49).</summary>
    private static IReadOnlyCollection<string> ChannelsOf(ReminderConfiguration configuration) =>
        configuration.Channel == ReminderChannels.Both
            ? ReminderChannels.Single
            : new[] { configuration.Channel };
}
