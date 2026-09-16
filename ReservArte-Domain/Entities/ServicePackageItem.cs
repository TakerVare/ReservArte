namespace ReservArte.Domain.Entities;

/// <summary>
/// Servicio incluido en un paquete, con su posición en la secuencia (vol. 1
/// §3.1.4: los combos pueden ser servicios secuenciales).
/// </summary>
public class ServicePackageItem
{
    public int Id { get; set; }

    /// <summary>
    /// Tenant propietario. Redundante con ServicePackage.OrganizationId a
    /// propósito: permite el query filter global sin depender de un JOIN
    /// (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int ServicePackageId { get; set; }
    public int ServiceId { get; set; }

    /// <summary>Orden de prestación dentro del paquete.</summary>
    public int Order { get; set; }

    /// <summary>Baja lógica: las líneas se desactivan, nunca se borran.</summary>
    public bool IsActive { get; set; } = true;

    public ServicePackage ServicePackage { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
