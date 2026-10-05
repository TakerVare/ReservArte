using Hangfire;
using ReservArte.Application.Interfaces;

namespace ReservArte.Infrastructure.Jobs;

/// <summary>Cola de avisos sobre Hangfire (RA-869d7f5zq).</summary>
public class HangfireReminderJobScheduler : IReminderJobScheduler
{
    private readonly IBackgroundJobClient _client;

    public HangfireReminderJobScheduler(IBackgroundJobClient client) => _client = client;

    public void Schedule(ReminderJobKey key, DateTimeOffset sendAt) =>
        _client.Schedule<ReminderJob>(
            job => job.RunAsync(key.OrganizationId, key.AppointmentId, key.ReminderConfigurationId, key.Channel),
            sendAt);
}
