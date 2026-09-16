using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;

namespace ReservArte.Domain.Interfaces;

/// <summary>
/// Criterios de búsqueda de paquetes. Todos opcionales: sin ninguno, devuelve
/// los paquetes activos del tenant actual.
/// </summary>
public class ServicePackageFilter
{
    /// <summary>Busca en nombre y descripción (contiene).</summary>
    public string? Search { get; init; }

    /// <summary>
    /// Null devuelve solo los activos: la baja es lógica y el catálogo de
    /// gestión no debe arrastrar bajas salvo que se pidan explícitamente.
    /// </summary>
    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Acceso a datos de los paquetes del catálogo (RA-869d7f45n). Separado de
/// <see cref="IServiceRepository"/> porque los paquetes son un recurso HTTP
/// propio (`/api/v1/service-packages`) y aquella interfaz ya cubre servicios,
/// categorías, variaciones y tarifas.
///
/// Todas las operaciones quedan acotadas al tenant actual; sin organización
/// resuelta no devuelven nada.
/// </summary>
public interface IServicePackageRepository
{
    /// <summary>
    /// Lista paginada con sus líneas y el servicio de cada una: la ficha
    /// necesita los precios y duraciones para calcular el total del paquete.
    /// </summary>
    Task<PagedResult<ServicePackage>> GetPagedAsync(
        ServicePackageFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paquete del tenant actual, **rastreado** y sin colecciones: para editar
    /// o dar de baja. Null si no existe o es de otro centro.
    /// </summary>
    Task<ServicePackage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paquete con sus líneas y el servicio de cada una, ordenadas por `Order`.
    /// Solo lectura.
    /// </summary>
    Task<ServicePackage?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    void Add(ServicePackage package);

    void Update(ServicePackage package);

    /// <summary>
    /// Sustituye TODAS las líneas del paquete por las indicadas, en una sola
    /// operación: el `PUT` reemplaza la composición entera, y así no quedan
    /// estados intermedios incoherentes mientras se edita.
    ///
    /// Mismo criterio que `ReplaceAvailabilitiesAsync` de Empleados, incluido el
    /// borrado físico de las líneas anteriores: una línea de composición no es
    /// histórico de negocio (ninguna cita apunta a ella), así que dejarla como
    /// baja lógica solo acumularía filas muertas.
    /// </summary>
    Task ReplaceItemsAsync(
        int packageId,
        IEnumerable<ServicePackageItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// De los ids indicados, los que existen como servicio **en este centro**.
    /// Se consulta en bloque para validar la composición entera con una sola
    /// consulta, en lugar de una por línea.
    /// </summary>
    Task<IReadOnlyCollection<int>> ExistingServiceIdsAsync(
        IEnumerable<int> serviceIds, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
