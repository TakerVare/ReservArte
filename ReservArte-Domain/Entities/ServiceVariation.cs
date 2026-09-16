namespace ReservArte.Domain.Entities;

/// <summary>
/// Variante de un servicio (tamaño, técnica…). Ajusta precio y duración del
/// servicio base con modificadores que se suman, de forma que cambiar el
/// servicio arrastre a todas sus variantes en lugar de dejarlas desfasadas.
/// </summary>
public class ServiceVariation
{
    public int Id { get; set; }

    /// <summary>
    /// Tenant propietario. Redundante con Service.OrganizationId a propósito:
    /// permite el query filter global sin depender de un JOIN (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Se suma a <see cref="Service.BasePrice"/>. Negativo abarata la variante.
    /// </summary>
    public decimal PriceModifier { get; set; }

    /// <summary>
    /// Minutos que se suman a <see cref="Service.DurationMinutes"/>. Negativo
    /// acorta la variante.
    /// </summary>
    public int DurationModifier { get; set; }

    /// <summary>Baja lógica: las variaciones se desactivan, nunca se borran.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Service Service { get; set; } = null!;
    public Organization Organization { get; set; } = null!;

    // Las líneas de cita que eligen esta variación llegan con el módulo de
    // Citas, no antes.
}
