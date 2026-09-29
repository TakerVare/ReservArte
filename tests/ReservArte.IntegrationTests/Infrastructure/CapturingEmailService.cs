using System.Collections.Concurrent;
using ReservArte.Application.Interfaces;

namespace ReservArte.IntegrationTests.Infrastructure;

/// <summary>
/// Sustituto de <see cref="IEmailService"/>: guarda los correos en memoria para
/// que los tests puedan comprobarlos y no deja ficheros en ./sent-emails/.
/// </summary>
public sealed class CapturingEmailService : IEmailService
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyCollection<EmailMessage> Sent => _sent.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
