namespace ReservArte.Domain.Entities;

/// <summary>
/// Cliente apuntado a la espera de que se libere un hueco para un servicio
/// (vol. 1 §3.1.5.E). Puede pedir una empleada concreta y una fecha preferida,
/// siempre dentro de un rango.
///
/// Se alinea en RA-869d7f4f1; su repositorio, servicio y endpoints llegan con
/// RA-869f2yh9b.
/// </summary>
public class WaitingList
{
    public int Id { get; set; }

    public Guid OrganizationId { get; set; }

    public int CustomerId { get; set; }
    public int ServiceId { get; set; }

    /// <summary>Empleada preferida; null si le vale cualquiera.</summary>
    public int? PreferredEmployeeId { get; set; }

    /// <summary>Fecha preferida dentro del rango; null si le vale cualquiera.</summary>
    public DateTime? PreferredDate { get; set; }

    public DateTime DateRangeStart { get; set; }
    public DateTime DateRangeEnd { get; set; }

    /// <summary>
    /// Orden de atención: **menor va antes**. El diseño quiere combinarlo con el
    /// orden de registro y la categoría del cliente (VIP primero); hoy nace en
    /// 1000 para dejar hueco por encima y por debajo sin renumerar la lista.
    /// </summary>
    public int Priority { get; set; } = 1000;

    /// <summary>Baja lógica: se apuntó y ya no le interesa, pero queda el rastro.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Cuándo se le avisó de un hueco libre. El envío en sí es del sistema de
    /// recordatorios (RA-869d7edh9); aquí solo queda la constancia.
    /// </summary>
    public DateTime? NotifiedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public Employee? PreferredEmployee { get; set; }
}
