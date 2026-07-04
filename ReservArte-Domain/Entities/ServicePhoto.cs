namespace ReservArte.Domain.Entities;

public class ServicePhoto
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string CloudinaryPublicId { get; set; } = string.Empty;
    public string CloudinarySecureUrl { get; set; } = string.Empty;
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublic { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Appointment Appointment { get; set; } = null!;
    public Employee UploadedByEmployee { get; set; } = null!;
}
