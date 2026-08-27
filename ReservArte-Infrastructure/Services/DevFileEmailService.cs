using Microsoft.Extensions.Logging;
using ReservArte.Application.Interfaces;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Implementación de desarrollo de IEmailService: NO envía email real; escribe
/// cada mensaje a un archivo en ./sent-emails/ (relativo al directorio de
/// ejecución de la API) para poder inspeccionarlo en local, incluido el
/// enlace/token de reset. En producción se usa la implementación de SES. El
/// token NO se registra en logs: solo se escribe en el archivo local, que
/// está fuera de control de versiones.
/// </summary>
public class DevFileEmailService : IEmailService
{
    private readonly string _outputDirectory;
    private readonly ILogger<DevFileEmailService> _logger;

    public DevFileEmailService(ILogger<DevFileEmailService> logger)
    {
        _outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "sent-emails");
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_outputDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        var safeRecipient = string.Concat(message.To.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{timestamp}_{safeRecipient}.txt";
        var path = Path.Combine(_outputDirectory, fileName);

        var contents =
            $"To: {message.To}\n" +
            $"Subject: {message.Subject}\n" +
            $"IsHtml: {message.IsHtml}\n" +
            $"Date: {DateTime.UtcNow:O}\n" +
            "----------------------------------------\n" +
            message.Body + "\n";

        await File.WriteAllTextAsync(path, contents, cancellationToken);

        // Log SIN el cuerpo (que contiene el token): solo constancia del envío.
        _logger.LogInformation(
            "[DEV] Email escrito a archivo para {Recipient} (asunto: {Subject})",
            message.To, message.Subject);
    }
}