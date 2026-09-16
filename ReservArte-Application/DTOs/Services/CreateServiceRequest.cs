namespace ReservArte.Application.DTOs.Services;

/// <summary>Alta de un servicio del catálogo.</summary>
public class CreateServiceRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }

    /// <summary>Minutos; debe ser mayor que 0 o la cita no ocuparía hueco.</summary>
    public int DurationMinutes { get; init; }

    public decimal BasePrice { get; init; }

    /// <summary>Categoría del catálogo. Null deja el servicio sin clasificar.</summary>
    public int? CategoryId { get; init; }

    public string? ImageUrl { get; init; }

    /// <summary>Exige prueba de alergia previa (vol. 1 §3.1.4).</summary>
    public bool RequiresAllergyTest { get; init; }

    /// <summary>
    /// Antelación de la prueba de alergia. Solo se aplica si
    /// <see cref="RequiresAllergyTest"/>; por defecto, las 48 horas del producto.
    /// </summary>
    public int AllergyTestHoursBefore { get; init; } = 48;
}
