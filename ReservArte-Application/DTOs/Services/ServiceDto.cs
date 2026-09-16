namespace ReservArte.Application.DTOs.Services;

/// <summary>Servicio del catálogo tal como lo expone la API en listados.</summary>
public class ServiceDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }

    /// <summary>Duración estimada; de aquí sale la hora de fin de la cita.</summary>
    public int DurationMinutes { get; init; }

    /// <summary>Precio de referencia; las tarifas por nivel lo sustituyen.</summary>
    public decimal BasePrice { get; init; }

    public int? CategoryId { get; init; }

    /// <summary>Nombre de la categoría, para no obligar a una segunda llamada.</summary>
    public string? CategoryName { get; init; }

    public string? ImageUrl { get; init; }
    public bool RequiresAllergyTest { get; init; }
    public int AllergyTestHoursBefore { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // OrganizationId NO se expone, mismo criterio que EmployeeDto y CustomerDto:
    // el tenant lo resuelve el servidor.
}

/// <summary>
/// Servicio con sus variaciones y sus tarifas por nivel vigentes. Los paquetes
/// que lo incluyen llegan con RA-869d7f45n.
/// </summary>
public class ServiceDetailDto : ServiceDto
{
    public IReadOnlyList<ServiceVariationDto> Variations { get; init; } = [];
    public IReadOnlyList<ServicePricingDto> Pricings { get; init; } = [];
}

/// <summary>Variante de un servicio. Los modificadores ajustan el servicio base.</summary>
public class ServiceVariationDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    /// <summary>Se suma al precio base; negativo lo abarata.</summary>
    public decimal PriceModifier { get; init; }

    /// <summary>Minutos que se suman a la duración base; negativo la acorta.</summary>
    public int DurationModifier { get; init; }
}

/// <summary>Tarifa del servicio para un nivel de empleada.</summary>
public class ServicePricingDto
{
    public int Id { get; init; }

    /// <summary>Valor de `EmployeeLevels`.</summary>
    public string EmployeeLevel { get; init; } = string.Empty;

    /// <summary>Precio final para ese nivel, no un recargo sobre el base.</summary>
    public decimal Price { get; init; }
}

/// <summary>Familia del catálogo con la que se agrupan los servicios.</summary>
public class ServiceCategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }

    /// <summary>Color con el que la agenda distingue esta familia.</summary>
    public string? Color { get; init; }

    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}
