namespace ReservArte.Domain.Entities;

/// <summary>
/// Combo de servicios con precio cerrado. Los servicios que lo componen y el
/// orden en que se prestan están en <see cref="ServicePackageItem"/>.
/// </summary>
public class ServicePackage
{
    public int Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Precio del paquete completo, ya con el descuento aplicado.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Descuento sobre la suma de los servicios sueltos, en tanto por ciento.
    /// Es informativo para la ficha: lo que se cobra es <see cref="TotalPrice"/>.
    /// </summary>
    public decimal DiscountPercentage { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Baja lógica: los paquetes se desactivan, nunca se borran.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;

    public ICollection<ServicePackageItem> Items { get; set; } =
        new List<ServicePackageItem>();

    // Promotions llega con el módulo de promociones, no antes.
}
