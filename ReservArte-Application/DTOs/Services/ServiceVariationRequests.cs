namespace ReservArte.Application.DTOs.Services;

/// <summary>
/// Alta de una variante de un servicio. Los modificadores ajustan el servicio
/// base; el servicio al que pertenece va en la ruta, no en el cuerpo.
/// </summary>
public class CreateServiceVariationRequest
{
    public string Name { get; init; } = string.Empty;

    /// <summary>Se suma al precio base; negativo abarata la variante.</summary>
    public decimal PriceModifier { get; init; }

    /// <summary>Minutos que se suman a la duración base; negativo la acorta.</summary>
    public int DurationModifier { get; init; }
}

/// <summary>
/// Edición de una variante. Sin `IsActive`: retirarla es `DELETE`, que es baja
/// lógica.
/// </summary>
public class UpdateServiceVariationRequest
{
    public string Name { get; init; } = string.Empty;
    public decimal PriceModifier { get; init; }
    public int DurationModifier { get; init; }
}
