namespace ReservArte.Application.DTOs.Services;

/// <summary>Alta de una familia del catálogo.</summary>
public class CreateServiceCategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }

    /// <summary>
    /// Color con el que la agenda distingue esta familia. Es un dato de negocio
    /// que elige el centro, no un token de tema: la identidad de marca sigue
    /// viniendo de las variables CSS.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>Posición en los listados del catálogo.</summary>
    public int DisplayOrder { get; init; }
}

/// <summary>
/// Edición de una categoría. No lleva `IsActive`: la baja y la reactivación son
/// operaciones propias, como en el resto del módulo.
/// </summary>
public class UpdateServiceCategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Color { get; init; }
    public int DisplayOrder { get; init; }
}
