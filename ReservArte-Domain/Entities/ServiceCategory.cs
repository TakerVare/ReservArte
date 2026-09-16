namespace ReservArte.Domain.Entities;

/// <summary>
/// Familia del catálogo con la que se agrupan y presentan los servicios. La
/// categoría es opcional en el servicio: uno sin clasificar sigue siendo válido.
/// </summary>
public class ServiceCategory
{
    public int Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Color con el que la agenda distingue esta familia. Es un dato de negocio
    /// que elige cada centro, no un token de tema: la identidad de marca sigue
    /// viniendo de las variables CSS.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>Posición en los listados del catálogo.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Baja lógica: las categorías se desactivan, nunca se borran.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<Service> Services { get; set; } = new List<Service>();
}
