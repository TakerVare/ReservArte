using ReservArte.Domain.Entities;

namespace ReservArte.Domain.Interfaces;

/// <summary>
/// Configuración del centro de la petición (RA-869f74u7y). Como el resto de
/// repositorios, no recibe la organización por parámetro: la toma de
/// <c>ICurrentOrganizationService</c>.
/// </summary>
public interface IOrganizationSettingsRepository
{
    /// <summary>La configuración guardada, sin seguimiento, o null si el centro aún no tiene fila.</summary>
    Task<OrganizationSettings?> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>La configuración guardada, con seguimiento, para editarla.</summary>
    Task<OrganizationSettings?> GetCurrentForUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>Alta de la fila del centro actual: el repositorio impone la organización.</summary>
    void Add(OrganizationSettings settings);

    void Update(OrganizationSettings settings);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
