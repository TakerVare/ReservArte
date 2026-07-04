namespace ReservArte.Domain.Entities;

public class ReminderConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int OrganizationId { get; set; }
    public int ReminderOrder { get; set; }
    public int HoursBeforeAppointment { get; set; }
    public string Channel { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid MessageTemplateId { get; set; }
    public TimeOnly? AllowedSendStartTime { get; set; }
    public TimeOnly? AllowedSendEndTime { get; set; }

    public MessageTemplate MessageTemplate { get; set; } = null!;
    public ICollection<ReminderLog> ReminderLogs { get; set; } = [];
}
