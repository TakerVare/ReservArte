namespace ReservArte.Domain.Entities;

/// <summary>
/// Un recordatorio que el centro envía antes de cada cita (RA-869d7f5wx): con
/// cuánta antelación, por qué canal y con qué plantilla. Un centro puede tener
/// varios (por ejemplo, 48 h y 2 h antes), ordenados por <see cref="ReminderOrder"/>.
/// </summary>
public class ReminderConfiguration
{
    public int Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>Posición entre los recordatorios del centro, desde 1.</summary>
    public int ReminderOrder { get; set; }

    public int HoursBeforeAppointment { get; set; }

    /// <summary>Por dónde se envía; valores de <see cref="ReminderChannels"/>.</summary>
    public string Channel { get; set; } = ReminderChannels.Email;

    public bool IsActive { get; set; } = true;

    public int MessageTemplateId { get; set; }

    /// <summary>
    /// Franja del día, en hora local del centro, en la que se permite enviar.
    /// Las dos horas o ninguna: sin franja, a cualquier hora.
    /// </summary>
    public TimeOnly? AllowedSendStartTime { get; set; }

    public TimeOnly? AllowedSendEndTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public MessageTemplate MessageTemplate { get; set; } = null!;
}

/// <summary>
/// Canales de un recordatorio. <see cref="Both"/> solo vale en la configuración:
/// cada envío (<see cref="ReminderLog"/>) va por un único canal de
/// <see cref="Single"/>.
/// </summary>
public static class ReminderChannels
{
    public const string Email = "email";
    public const string WhatsApp = "whatsapp";

    /// <summary>Email y WhatsApp: genera un envío por cada uno.</summary>
    public const string Both = "both";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Email,
        WhatsApp,
        Both,
    };

    /// <summary>Los canales por los que sale un envío concreto.</summary>
    public static readonly IReadOnlyCollection<string> Single = new[]
    {
        Email,
        WhatsApp,
    };
}
