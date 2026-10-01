namespace ReservArte.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "ES";
    public string? TaxId { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Semanas hacia delante en las que una clienta puede reservar desde hoy (H-44).
    /// Configurable por centro; la pantalla de Configuración llegará después.
    /// </summary>
    public int CustomerBookingWindowWeeks { get; set; } = 6;

    /// <summary>Semanas hacia delante que el personal ve en la pantalla de reserva (H-44).</summary>
    public int StaffBookingWindowWeeks { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = [];
}
