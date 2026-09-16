using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;

namespace ReservArte.Domain.Interfaces;

/// <summary>
/// Criterios de búsqueda del catálogo. Todos opcionales: sin ninguno, devuelve
/// los servicios activos del tenant actual.
/// </summary>
public class ServiceFilter
{
    /// <summary>Busca en nombre y descripción (contiene).</summary>
    public string? Search { get; init; }

    /// <summary>Categoría concreta. Null = cualquiera, con o sin categoría.</summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// Null devuelve solo los activos: la baja es lógica y el catálogo de
    /// gestión no debe arrastrar bajas salvo que se pidan explícitamente.
    /// Mismo criterio que <see cref="CustomerFilter"/>.
    /// </summary>
    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Acceso a datos del catálogo de servicios (RA-869d7f3z0). Todas las
/// operaciones quedan acotadas al tenant actual; sin organización resuelta no
/// devuelven nada.
///
/// Los paquetes (`ServicePackages`) están mapeados pero no se exponen aquí: sus
/// casos de uso llegan con RA-869d7f45n. Mismo criterio que
/// <see cref="ICustomerRepository"/>, que nació sin el historial de citas.
/// </summary>
public interface IServiceRepository
{
    /// <summary>Lista paginada con filtros, ordenada por nombre.</summary>
    Task<PagedResult<Service>> GetPagedAsync(
        ServiceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Servicio del tenant actual, o null si no existe.</summary>
    Task<Service?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Servicio con su categoría, sus variaciones y sus tarifas por nivel
    /// vigentes (las dadas de baja no se cargan). Solo lectura.
    /// </summary>
    Task<Service?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    void Add(Service service);

    void Update(Service service);

    /// <summary>Categorías del tenant, en su orden de presentación.</summary>
    Task<IReadOnlyList<ServiceCategory>> GetCategoriesAsync(
        bool? isActive = null, CancellationToken cancellationToken = default);

    /// <summary>Categoría del tenant actual, o null si no existe.</summary>
    Task<ServiceCategory?> GetCategoryByIdAsync(
        int id, CancellationToken cancellationToken = default);

    void AddCategory(ServiceCategory category);

    void UpdateCategory(ServiceCategory category);

    /// <summary>
    /// Variación del servicio indicado, vigente o retirada, o null si no
    /// existe, es de otro servicio o de otro centro.
    /// </summary>
    Task<ServiceVariation?> GetVariationAsync(
        int serviceId, int variationId, CancellationToken cancellationToken = default);

    void AddVariation(ServiceVariation variation);

    void UpdateVariation(ServiceVariation variation);

    /// <summary>Tarifa vigente del servicio para ese nivel, o null.</summary>
    Task<ServicePricing?> GetPricingAsync(
        int serviceId, string employeeLevel, CancellationToken cancellationToken = default);

    void AddPricing(ServicePricing pricing);

    void UpdatePricing(ServicePricing pricing);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
