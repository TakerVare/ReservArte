namespace ReservArte.Application.Interfaces;

/// <summary>
/// Mensaje de email neutral, independiente del proveedor. Cualquier
/// implementación (desarrollo a archivo, SES en producción, SMTP...) lo
/// entiende. El cuerpo se pasa ya construido (no se usan plantillas nativas
/// del proveedor), para máxima portabilidad.
/// </summary>
public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
}

/// <summary>
/// Abstracción de envío de email. La implementación se resuelve por entorno
/// (desarrollo: escribe a archivo; producción: SES — tarea futura). El resto
/// del código depende solo de esta interfaz: cambiar de proveedor es cambiar
/// el registro DI, no la lógica de negocio.
/// </summary>
public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}