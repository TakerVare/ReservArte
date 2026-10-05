using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Persistence.Repositories;

/// <summary>Configuración del centro de la petición (RA-869f74u7y).</summary>
public class OrganizationSettingsRepository : IOrganizationSettingsRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationService _currentOrganization;

    public OrganizationSettingsRepository(AppDbContext context, ICurrentOrganizationService currentOrganization)
    {
        _context = context;
        _currentOrganization = currentOrganization;
    }

    public Task<OrganizationSettings?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        Current().AsNoTracking().FirstOrDefaultAsync(cancellationToken);

    public Task<OrganizationSettings?> GetCurrentForUpdateAsync(CancellationToken cancellationToken = default) =>
        Current().FirstOrDefaultAsync(cancellationToken);

    public void Add(OrganizationSettings settings)
    {
        // El tenant se impone aquí: una petición no puede crear la configuración
        // de otro centro.
        settings.OrganizationId = _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException(
                "No se puede guardar la configuración sin organización resuelta.");

        _context.OrganizationSettings.Add(settings);
    }

    public void Update(OrganizationSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _context.OrganizationSettings.Update(settings);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Segunda barrera además del query filter: sin tenant, ninguna fila; con
    /// él, solo la suya (también si hubiera un ámbito de sistema abierto).
    /// </summary>
    private IQueryable<OrganizationSettings> Current() =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.OrganizationSettings.Where(s => s.OrganizationId == organizationId)
            : _context.OrganizationSettings.Where(_ => false);
}
