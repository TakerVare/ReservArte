using System.Collections.Concurrent;
using ReservArte.Application.Interfaces;

namespace ReservArte.IntegrationTests.Infrastructure;

/// <summary>
/// Sustituye a la cola de Hangfire (RA-869d7f5zq): guarda cada aviso programado
/// con su hora, para comprobarlo sin esperar a que dispare.
/// </summary>
public sealed class CapturingReminderJobScheduler : IReminderJobScheduler
{
    private readonly ConcurrentQueue<(ReminderJobKey Key, DateTimeOffset SendAt)> _scheduled = new();

    public void Schedule(ReminderJobKey key, DateTimeOffset sendAt) => _scheduled.Enqueue((key, sendAt));

    /// <summary>Avisos programados para esa cita, en el orden en que se programaron.</summary>
    public IReadOnlyList<(ReminderJobKey Key, DateTimeOffset SendAt)> For(int appointmentId) =>
        _scheduled.Where(s => s.Key.AppointmentId == appointmentId).ToList();
}
