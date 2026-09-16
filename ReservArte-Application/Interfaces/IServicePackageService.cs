using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Services;
using ReservArte.Domain.Common;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Casos de uso de los paquetes del catálogo (RA-869d7f45n). Todas las
/// operaciones quedan acotadas al tenant de la petición.
///
/// Separado de <see cref="IServiceCatalogService"/> porque los paquetes son un
/// recurso HTTP propio y aquel servicio ya cubre servicios, categorías,
/// variaciones y tarifas.
/// </summary>
public interface IServicePackageService
{
    /// <summary>Lista paginada con búsqueda; cada paquete lleva sus líneas.</summary>
    Task<Result<PagedResult<ServicePackageDto>>> GetPagedAsync(
        ServicePackageFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Paquete con sus líneas. Otro centro o inexistente → GEN_NOT_FOUND.</summary>
    Task<Result<ServicePackageDto>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alta con su composición. Un `serviceId` que no exista en el centro es
    /// GEN_VALIDATION_FAILED (`field = items[i].serviceId`), no un 404: el
    /// recurso que se está creando es el paquete.
    /// </summary>
    Task<Result<ServicePackageDto>> CreateAsync(
        CreateServicePackageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edita el paquete y **reemplaza su composición entera**. No toca la baja:
    /// eso son operaciones propias.
    /// </summary>
    Task<Result<ServicePackageDto>> UpdateAsync(
        int id, UpdateServicePackageRequest request, CancellationToken cancellationToken = default);

    /// <summary>Baja lógica, idempotente. El paquete deja de ofrecerse.</summary>
    Task<Result<ServicePackageDto>> DeactivateAsync(
        int id, CancellationToken cancellationToken = default);

    /// <summary>Reactiva un paquete dado de baja.</summary>
    Task<Result<ServicePackageDto>> ReactivateAsync(
        int id, CancellationToken cancellationToken = default);
}
