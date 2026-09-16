using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core de <see cref="IServiceRepository"/> (RA-869d7f3z0).</summary>
public class ServiceRepository : IServiceRepository
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationService _currentOrganization;

    public ServiceRepository(
        AppDbContext context, ICurrentOrganizationService currentOrganization)
    {
        _context = context;
        _currentOrganization = currentOrganization;
    }

    /// <summary>
    /// Servicios del tenant actual. El query filter global ya acota por
    /// organización; este filtro explícito cubre lo que el global no: sin
    /// organización resuelta el global deja pasar todo (lo necesitan migraciones
    /// y seeders), y aquí es preferible no devolver nada. Mismo criterio que
    /// CustomerRepository y EmployeeRepository.
    /// </summary>
    private IQueryable<Service> TenantServices =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.Services.Where(s => s.OrganizationId == organizationId)
            : _context.Services.Where(_ => false);

    private IQueryable<ServiceCategory> TenantCategories =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.ServiceCategories.Where(c => c.OrganizationId == organizationId)
            : _context.ServiceCategories.Where(_ => false);

    private IQueryable<ServiceVariation> TenantVariations =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.ServiceVariations.Where(v => v.OrganizationId == organizationId)
            : _context.ServiceVariations.Where(_ => false);

    private IQueryable<ServicePricing> TenantPricings =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.ServicePricings.Where(p => p.OrganizationId == organizationId)
            : _context.ServicePricings.Where(_ => false);

    public async Task<PagedResult<Service>> GetPagedAsync(
        ServiceFilter filter, CancellationToken cancellationToken = default)
    {
        var query = TenantServices.Where(s => s.IsActive == (filter.IsActive ?? true));

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(s => s.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(s =>
                EF.Functions.Like(s.Name, $"%{search}%") ||
                (s.Description != null && EF.Functions.Like(s.Description, $"%{search}%")));
        }

        // Se cuenta antes de paginar: el total es el del filtro, no el de la página.
        var totalCount = await query.CountAsync(cancellationToken);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        var items = await query
            .Include(s => s.Category)
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResult<Service>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public Task<Service?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        TenantServices.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Service?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        TenantServices
            .Include(s => s.Category)
            .Include(s => s.Variations.Where(v => v.IsActive).OrderBy(v => v.Name))
            .Include(s => s.Pricings.Where(p => p.IsActive))
            // Dos colecciones en una sola consulta multiplicarían las filas.
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(Service service) => _context.Services.Add(service);

    public void Update(Service service)
    {
        service.UpdatedAt = DateTime.UtcNow;
        _context.Services.Update(service);
    }

    public async Task<IReadOnlyList<ServiceCategory>> GetCategoriesAsync(
        bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = TenantCategories.AsQueryable();

        if (isActive is { } activo)
        {
            query = query.Where(c => c.IsActive == activo);
        }

        return await query
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<ServiceCategory?> GetCategoryByIdAsync(
        int id, CancellationToken cancellationToken = default) =>
        TenantCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void AddCategory(ServiceCategory category) => _context.ServiceCategories.Add(category);

    public void UpdateCategory(ServiceCategory category)
    {
        category.UpdatedAt = DateTime.UtcNow;
        _context.ServiceCategories.Update(category);
    }

    public Task<ServiceVariation?> GetVariationAsync(
        int serviceId, int variationId, CancellationToken cancellationToken = default) =>
        TenantVariations.FirstOrDefaultAsync(
            v => v.ServiceId == serviceId && v.Id == variationId, cancellationToken);

    public void AddVariation(ServiceVariation variation) =>
        _context.ServiceVariations.Add(variation);

    public void UpdateVariation(ServiceVariation variation)
    {
        variation.UpdatedAt = DateTime.UtcNow;
        _context.ServiceVariations.Update(variation);
    }

    /// <summary>
    /// Tarifa VIGENTE del nivel: el índice único solo cubre las activas, así que
    /// una retirada no impide crear otra para el mismo nivel.
    /// </summary>
    public Task<ServicePricing?> GetPricingAsync(
        int serviceId, string employeeLevel, CancellationToken cancellationToken = default) =>
        TenantPricings.FirstOrDefaultAsync(
            p => p.ServiceId == serviceId && p.EmployeeLevel == employeeLevel && p.IsActive,
            cancellationToken);

    public void AddPricing(ServicePricing pricing) => _context.ServicePricings.Add(pricing);

    public void UpdatePricing(ServicePricing pricing)
    {
        pricing.UpdatedAt = DateTime.UtcNow;
        _context.ServicePricings.Update(pricing);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
