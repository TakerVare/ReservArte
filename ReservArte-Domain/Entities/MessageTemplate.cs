namespace ReservArte.Domain.Entities;

public class MessageTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ReminderConfiguration> ReminderConfigurations { get; set; } = [];
}
