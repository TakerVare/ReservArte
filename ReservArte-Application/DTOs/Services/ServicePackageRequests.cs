namespace ReservArte.Application.DTOs.Services;

/// <summary>Servicio que se incluye en un paquete, al crearlo o editarlo.</summary>
public class ServicePackageItemRequest
{
    public int ServiceId { get; init; }

    /// <summary>Orden de prestación dentro del paquete.</summary>
    public int Order { get; init; }
}

/// <summary>
/// Alta de un paquete con los servicios que lo componen. `TotalPrice` es el
/// precio cerrado que se cobra: no se calcula a partir del descuento, porque el
/// centro puede redondear o pactar otro importe.
/// </summary>
public class CreateServicePackageRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }

    /// <summary>Lo que se cobra por el paquete completo.</summary>
    public decimal TotalPrice { get; init; }

    /// <summary>Descuento declarado, en tanto por ciento (0-100). Informativo.</summary>
    public decimal DiscountPercentage { get; init; }

    /// <summary>
    /// Servicios incluidos. Obligatorio y sin repetidos: un paquete sin
    /// servicios no es un paquete, y repetir uno haría ambiguo su orden.
    /// </summary>
    public IReadOnlyList<ServicePackageItemRequest> Items { get; init; } = [];
}

/// <summary>
/// Edición de un paquete. La lista de `items` **reemplaza la anterior entera**,
/// igual que el horario semanal de Empleados (`PUT …/availability`): es la forma
/// natural de editar una composición en un formulario, y evita tener que
/// exponer endpoints por línea.
///
/// No lleva `IsActive`: la baja es una operación propia.
/// </summary>
public class UpdateServicePackageRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public decimal TotalPrice { get; init; }
    public decimal DiscountPercentage { get; init; }
    public IReadOnlyList<ServicePackageItemRequest> Items { get; init; } = [];
}
