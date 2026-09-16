namespace ReservArte.Domain.Entities;

/// <summary>
/// Servicio del catálogo del centro (diseño de cejas, tinte…). Su duración y
/// su precio base son la fuente de la que salen la hora de fin y el importe de
/// una cita: las variaciones y las tarifas por nivel los ajustan, no los
/// sustituyen.
/// </summary>
public class Service
{
    public int Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Duración estimada. La cita la usa para calcular su hora de fin, sumándole
    /// el <see cref="ServiceVariation.DurationModifier"/> si hay variación.
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Precio de referencia. <see cref="ServicePricing"/> lo sustituye por nivel
    /// de la empleada y <see cref="ServiceVariation"/> lo ajusta por variante.
    /// </summary>
    public decimal BasePrice { get; set; }

    public int? CategoryId { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Baja lógica: los servicios se desactivan, nunca se borran.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Requisito previo del servicio (vol. 1 §3.1.4): exige prueba de alergia
    /// con <see cref="AllergyTestHoursBefore"/> horas de antelación.
    /// </summary>
    public bool RequiresAllergyTest { get; set; }

    public int AllergyTestHoursBefore { get; set; } = 48;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ServiceCategory? Category { get; set; }

    public ICollection<ServiceVariation> Variations { get; set; } =
        new List<ServiceVariation>();

    /// <summary>Tarifas por nivel de empleada; sin fila se cobra el precio base.</summary>
    public ICollection<ServicePricing> Pricings { get; set; } =
        new List<ServicePricing>();

    /// <summary>Empleadas capacitadas para prestarlo.</summary>
    public ICollection<EmployeeServiceAssignment> Employees { get; set; } =
        new List<EmployeeServiceAssignment>();

    public ICollection<ServicePackageItem> PackageItems { get; set; } =
        new List<ServicePackageItem>();

    // Products, Promotions, WaitingLists y las líneas de cita llegan con sus
    // propios módulos (inventario, promociones, lista de espera y citas), no
    // antes: hoy esas entidades no están en el DbContext. Mismo criterio que
    // Customer y Employee.
}
