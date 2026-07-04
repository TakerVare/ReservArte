namespace ReservArte.Domain.Entities;

public class ReminderLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int AppointmentId { get; set; }
    public Guid ReminderConfigurationId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "pending";
    public string? ExternalMessageId { get; set; }
    public string? ErrorMessage { get; set; }

    public Appointment Appointment { get; set; } = null!;
    public ReminderConfiguration ReminderConfiguration { get; set; } = null!;
}
