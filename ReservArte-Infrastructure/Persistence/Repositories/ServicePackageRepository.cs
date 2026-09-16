using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core de <see cref="IServicePackageRepository"/> (RA-869d7f45n).</summary>
public class ServicePackageRepository : IServicePackageRepository
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationService _currentOrganization;

    public ServicePackageRepository(
        AppDbContext context, ICurrentOrganizationService currentOrganization)
    {
        _context = context;
        _currentOrganization = currentOrganization;
    }

    /// <summary>
    /// Paquetes del tenant actual. El query filter global ya acota por
    /// organización; este filtro explícito cubre lo que el global no: sin
    /// organización resuelta el global deja pasar todo (lo necesitan migraciones
    /// y seeders), y aquí es preferible no devolver nada.
    /// </summary>
    private IQueryable<ServicePackage> TenantPackages =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.ServicePackages.Where(p => p.OrganizationId == organizationId)
            : _context.ServicePackages.Where(_ => false);

    private IQueryable<Service> TenantServices =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.Services.Where(s => s.OrganizationId == organizationId)
            : _context.Services.Where(_ => false);

    public async Task<PagedResult<ServicePackage>> GetPagedAsync(
        ServicePackageFilter filter, CancellationToken cancellationToken = default)
    {
        var query = TenantPackages.Where(p => p.IsActive == (filter.IsActive ?? true));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{search}%") ||
                (p.Description != null && EF.Functions.Like(p.Description, $"%{search}%")));
        }

        // Se cuenta antes de paginar: el total es el del filtro, no el de la página.
        var totalCount = await query.CountAsync(cancellationToken);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        var items = await query
            .Include(p => p.Items.OrderBy(i => i.Order))
                .ThenInclude(i => i.Service)
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResult<ServicePackage>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public Task<ServicePackage?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        TenantPackages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<ServicePackage?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        TenantPackages
            .Include(p => p.Items.OrderBy(i => i.Order))
                .ThenInclude(i => i.Service)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(ServicePackage package) => _context.ServicePackages.Add(package);

    public void Update(ServicePackage package)
    {
        package.UpdatedAt = DateTime.UtcNow;
        _context.ServicePackages.Update(package);
    }

    public async Task ReplaceItemsAsync(
        int packageId,
        IEnumerable<ServicePackageItem> items,
        CancellationToken cancellationToken = default)
    {
        var current = await _context.ServicePackageItems
            .Where(i => i.ServicePackageId == packageId)
            .ToListAsync(cancellationToken);

        _context.ServicePackageItems.RemoveRange(current);

        foreach (var item in items)
        {
            // Ni el paquete ni el tenant se toman de la entrada: se imponen aquí
            // para que una petición no pueda colar líneas en otro paquete ni en
            // otra organización (mismo criterio que ReplaceAvailabilitiesAsync).
            item.ServicePackageId = packageId;
            item.OrganizationId =
                _currentOrganization.OrganizationId ?? item.OrganizationId;

            _context.ServicePackageItems.Add(item);
        }
    }

    public async Task<IReadOnlyCollection<int>> ExistingServiceIdsAsync(
        IEnumerable<int> serviceIds, CancellationToken cancellationToken = default)
    {
        var ids = serviceIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        return await TenantServices
            .Where(s => ids.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
