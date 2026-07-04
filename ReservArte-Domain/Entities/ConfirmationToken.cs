namespace ReservArte.Domain.Entities;

public class ConfirmationToken
{
    public string Token { get; set; } = string.Empty;
    public int AppointmentId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Appointment Appointment { get; set; } = null!;
}
