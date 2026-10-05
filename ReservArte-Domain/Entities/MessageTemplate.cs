namespace ReservArte.Domain.Entities;

/// <summary>
/// Plantilla de un mensaje al cliente (RA-869d7f5wx): el texto de un recordatorio
/// o de una confirmación, con variables entre llaves dobles
/// (<c>{{customerName}}</c>, <c>{{appointmentDate}}</c>…) que se sustituyen al
/// enviar. Cada centro tiene las suyas.
/// </summary>
public class MessageTemplate
{
    public int Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Para qué sirve; valores de <see cref="MessageTemplateTypes"/>.</summary>
    public string Type { get; set; } = MessageTemplateTypes.EmailReminder;

    /// <summary>Asunto. Solo tiene sentido en las de email.</summary>
    public string? Subject { get; set; }

    public string Body { get; set; } = string.Empty;

    /// <summary>Idioma del texto, código ISO 639-1.</summary>
    public string Language { get; set; } = "es";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
}

/// <summary>Valores admitidos por <see cref="MessageTemplate.Type"/>.</summary>
public static class MessageTemplateTypes
{
    public const string EmailReminder = "email_reminder";
    public const string WhatsAppReminder = "whatsapp_reminder";
    public const string Confirmation = "confirmation";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        EmailReminder,
        WhatsAppReminder,
        Confirmation,
    };
}
