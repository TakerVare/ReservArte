using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Organizations;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Configuración del centro de la petición (RA-869f74u7y). Un centro sin
/// configuración guardada responde con los valores por defecto; la primera
/// escritura crea su fila.
/// </summary>
public interface IOrganizationSettingsService
{
    Task<Result<OrganizationSettingsDto>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Reemplaza la configuración entera (crea la fila si no existe).</summary>
    Task<Result<OrganizationSettingsDto>> UpdateAsync(
        UpdateOrganizationSettingsRequest request, CancellationToken cancellationToken = default);
}
