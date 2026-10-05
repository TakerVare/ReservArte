namespace ReservArte.Domain.Entities;

/// <summary>
/// Un envío de recordatorio a una cita por un canal (RA-869d7f5wx). Nace
/// <c>pending</c> al programarse y el mismo registro se actualiza con el
/// resultado: como mucho hay uno por cita, recordatorio y canal, que es lo que
/// impide enviar dos veces el mismo aviso si el job se repite.
/// </summary>
public class ReminderLog
{
    public int Id { get; set; }

    /// <summary>
    /// Tenant propietario. Redundante con el de la cita a propósito: permite el
    /// query filter global sin depender de un JOIN (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int AppointmentId { get; set; }
    public int ReminderConfigurationId { get; set; }

    /// <summary>Canal de este envío; valores de <see cref="ReminderChannels.Single"/>.</summary>
    public string Channel { get; set; } = ReminderChannels.Email;

    /// <summary>Valores de <see cref="ReminderLogStatuses"/>.</summary>
    public string Status { get; set; } = ReminderLogStatuses.Pending;

    /// <summary>Cuándo salió (UTC). Nulo mientras está pendiente o si falló antes de salir.</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>Identificador del mensaje en el proveedor (SES, WhatsApp), para casar sus avisos de entrega.</summary>
    public string? ExternalMessageId { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public Appointment Appointment { get; set; } = null!;
    public ReminderConfiguration ReminderConfiguration { get; set; } = null!;
}

/// <summary>Valores admitidos por <see cref="ReminderLog.Status"/>.</summary>
public static class ReminderLogStatuses
{
    /// <summary>Programado, sin enviar todavía. Estado inicial.</summary>
    public const string Pending = "pending";

    /// <summary>Entregado al proveedor.</summary>
    public const string Sent = "sent";

    public const string Failed = "failed";

    /// <summary>El proveedor confirma la entrega al destinatario.</summary>
    public const string Delivered = "delivered";

    public const string Opened = "opened";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Pending,
        Sent,
        Failed,
        Delivered,
        Opened,
    };
}
