using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Persistence.Repositories;

/// <summary>La organización de la petición (RA-869fagpx9).</summary>
public class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationService _currentOrganization;

    public OrganizationRepository(AppDbContext context, ICurrentOrganizationService currentOrganization)
    {
        _context = context;
        _currentOrganization = currentOrganization;
    }

    public Task<Organization?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.Organizations.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
            : Task.FromResult<Organization?>(null);
}
