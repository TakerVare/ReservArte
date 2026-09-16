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
/// Cubre el catálogo completo: servicios, categorías, variaciones y tarifas por
/// nivel (las escrituras de estas tres últimas, desde RA-869f2wtrk). Los
/// paquetes llegan con RA-869d7f45n.
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

    // ── Categorías (RA-869f2wtrk) ─────────────────────────────────────────

    Task<Result<ServiceCategoryDto>> CreateCategoryAsync(
        CreateServiceCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result<ServiceCategoryDto>> UpdateCategoryAsync(
        int categoryId, UpdateServiceCategoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Baja lógica, idempotente. **Se permite aunque tenga servicios**: como la
    /// baja es lógica, ninguno se queda sin clasificar y la categoría retirada
    /// sigue viajando en `GetCategoriesAsync` sin filtro, que es lo que permite
    /// al formulario de edición seguir mostrando la clasificación.
    /// </summary>
    Task<Result<ServiceCategoryDto>> DeactivateCategoryAsync(
        int categoryId, CancellationToken cancellationToken = default);

    Task<Result<ServiceCategoryDto>> ReactivateCategoryAsync(
        int categoryId, CancellationToken cancellationToken = default);

    // ── Variaciones (RA-869f2wtrk) ────────────────────────────────────────

    /// <summary>
    /// Añade una variante. Rechaza la que dejaría la duración resultante en cero
    /// o negativa (GEN_VALIDATION_FAILED, `field = durationModifier`): una cita
    /// con esa variante no ocuparía hueco en la agenda.
    /// </summary>
    Task<Result<ServiceVariationDto>> AddVariationAsync(
        int serviceId, CreateServiceVariationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ServiceVariationDto>> UpdateVariationAsync(
        int serviceId, int variationId, UpdateServiceVariationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Retira una variante (baja lógica, idempotente).</summary>
    Task<Result<ServiceVariationDto>> DeleteVariationAsync(
        int serviceId, int variationId, CancellationToken cancellationToken = default);

    // ── Tarifas por nivel (RA-869f2wtrk) ──────────────────────────────────

    /// <summary>
    /// Crea o actualiza la tarifa vigente del nivel. Es idempotente a propósito:
    /// el nivel es la clave natural y el índice único solo admite una vigente,
    /// así que un alta repetida actualiza en lugar de chocar. Un nivel fuera de
    /// `EmployeeLevels` es GEN_VALIDATION_FAILED (`field = employeeLevel`),
    /// antes de que salte el CHECK del esquema.
    /// </summary>
    Task<Result<ServicePricingDto>> SetPricingAsync(
        int serviceId, string employeeLevel, UpsertServicePricingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retira la tarifa vigente del nivel (baja lógica). Si no hay ninguna
    /// vigente → GEN_NOT_FOUND: el recurso es la tarifa vigente, y una retirada
    /// ya no se puede direccionar por su nivel. A diferencia de las variaciones,
    /// esta operación **no** es idempotente.
    /// </summary>
    Task<Result<ServicePricingDto>> DeletePricingAsync(
        int serviceId, string employeeLevel, CancellationToken cancellationToken = default);
}
