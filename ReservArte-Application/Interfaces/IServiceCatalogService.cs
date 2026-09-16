using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Services;
using ReservArte.Domain.Common;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Casos de uso del catálogo de servicios (RA-869d7f3z0). Todas las operaciones
/// quedan acotadas al tenant de la petición; el servicio nunca lo acepta como
/// parámetro.
///
/// Se llama ServiceCatalog y no `ServiceService`: el nombre que pedía ClickUp
/// tartamudea, y «catálogo» describe mejor el conjunto (servicios, categorías,
/// variaciones y tarifas por nivel). No es `CatalogService` a secas porque más
/// adelante habrá un catálogo de productos (inventario).
///
/// Las escrituras de categorías, variaciones y tarifas llegan con sus endpoints
/// (RA-869d7f42u); aquí las categorías solo se leen, para poder clasificar y
/// filtrar servicios.
/// </summary>
public interface IServiceCatalogService
{
    /// <summary>Lista paginada con búsqueda y filtros.</summary>
    Task<Result<PagedResult<ServiceDto>>> GetPagedAsync(
        ServiceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Servicio con sus variaciones y tarifas vigentes. Otro centro o
    /// inexistente → GEN_NOT_FOUND.
    /// </summary>
    Task<Result<ServiceDetailDto>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alta. Una categoría que no exista en el centro es
    /// GEN_VALIDATION_FAILED (`field = categoryId`), no un 404: el recurso que
    /// se está creando es el servicio.
    /// </summary>
    Task<Result<ServiceDetailDto>> CreateAsync(
        CreateServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Edita el servicio. No toca su baja: eso son operaciones propias.</summary>
    Task<Result<ServiceDto>> UpdateAsync(
        int id, UpdateServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Baja lógica, idempotente. El servicio deja de ofrecerse pero no
    /// desaparece: las citas ya cerradas siguen apuntando a él.
    /// </summary>
    Task<Result<ServiceDto>> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Reactiva un servicio dado de baja.</summary>
    Task<Result<ServiceDto>> ReactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Categorías del centro, en su orden de presentación.</summary>
    Task<Result<IReadOnlyList<ServiceCategoryDto>>> GetCategoriesAsync(
        bool? isActive = null, CancellationToken cancellationToken = default);
}
