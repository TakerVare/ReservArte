using ReservArte.Domain.Entities;

namespace ReservArte.Domain.Interfaces;

/// <summary>
/// La organización de la petición (RA-869fagpx9). Como el resto de repositorios,
/// no recibe el id por parámetro: lo toma de <c>ICurrentOrganizationService</c>.
/// </summary>
public interface IOrganizationRepository
{
    /// <summary>La organización actual, sin seguimiento, o null si no hay tenant resuelto.</summary>
    Task<Organization?> GetCurrentAsync(CancellationToken cancellationToken = default);
}
