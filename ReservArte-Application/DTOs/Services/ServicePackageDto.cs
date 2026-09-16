namespace ReservArte.Application.DTOs.Services;

/// <summary>
/// Combo de servicios con precio cerrado (RA-869d7f45n).
///
/// Sobre los importes: <see cref="TotalPrice"/> es lo que se cobra y ya lleva el
/// descuento aplicado; <see cref="DiscountPercentage"/> es informativo para la
/// ficha. Para que eso sea útil en pantalla hace falta el término de
/// comparación, así que se calculan <see cref="ItemsTotalPrice"/> y
/// <see cref="Savings"/> a partir de los servicios incluidos.
/// </summary>
public class ServicePackageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }

    /// <summary>Precio del paquete completo: lo que se cobra.</summary>
    public decimal TotalPrice { get; init; }

    /// <summary>
    /// Descuento declarado sobre la suma de los servicios sueltos, en tanto por
    /// ciento. Es informativo: el importe que manda es <see cref="TotalPrice"/>.
    /// </summary>
    public decimal DiscountPercentage { get; init; }

    /// <summary>
    /// Suma de los precios base de los servicios incluidos, calculada al leer.
    /// No se guarda: si cambia el precio de un servicio, este importe cambia
    /// solo, mientras que <see cref="TotalPrice"/> sigue siendo el pactado.
    /// </summary>
    public decimal ItemsTotalPrice { get; init; }

    /// <summary>
    /// Lo que se ahorra el cliente: <see cref="ItemsTotalPrice"/> menos
    /// <see cref="TotalPrice"/>. Puede salir negativo si el paquete se encareció
    /// o si bajaron los precios sueltos; no se recorta a cero a propósito, para
    /// que esa incoherencia se vea en lugar de esconderse.
    /// </summary>
    public decimal Savings { get; init; }

    /// <summary>Duración total del paquete, sumando la de sus servicios.</summary>
    public int TotalDurationMinutes { get; init; }

    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    /// <summary>Servicios incluidos, en su orden de prestación.</summary>
    public IReadOnlyList<ServicePackageItemDto> Items { get; init; } = [];

    // OrganizationId NO se expone, mismo criterio que el resto del módulo.
}

/// <summary>Servicio incluido en un paquete, con su posición en la secuencia.</summary>
public class ServicePackageItemDto
{
    public int Id { get; init; }
    public int ServiceId { get; init; }

    /// <summary>Nombre del servicio, para no obligar a una segunda llamada.</summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>Precio base del servicio suelto, en el momento de la consulta.</summary>
    public decimal BasePrice { get; init; }

    public int DurationMinutes { get; init; }

    /// <summary>Orden de prestación dentro del paquete.</summary>
    public int Order { get; init; }
}
