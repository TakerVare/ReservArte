using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core de <see cref="ICustomerRepository"/> (RA-869d7f32r).</summary>
public class CustomerRepository : ICustomerRepository
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationService _currentOrganization;

    public CustomerRepository(
        AppDbContext context, ICurrentOrganizationService currentOrganization)
    {
        _context = context;
        _currentOrganization = currentOrganization;
    }

    /// <summary>
    /// Clientes del tenant actual. El query filter global ya acota por
    /// organización; este filtro explícito cubre lo que el global no: sin
    /// organización resuelta el global deja pasar todo (lo necesitan migraciones
    /// y seeders), y aquí es preferible no devolver nada. Mismo criterio que
    /// EmployeeRepository.
    /// </summary>
    private IQueryable<Customer> TenantCustomers =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.Customers.Where(c => c.OrganizationId == organizationId)
            : _context.Customers.Where(_ => false);

    public async Task<PagedResult<Customer>> GetPagedAsync(
        CustomerFilter filter, CancellationToken cancellationToken = default)
    {
        var query = TenantCustomers.Where(c => c.IsActive == (filter.IsActive ?? true));

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(c => c.Category == filter.Category);
        }

        if (filter.IsBlocked is { } isBlocked)
        {
            query = query.Where(c => c.IsBlocked == isBlocked);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(c =>
                EF.Functions.Like(c.FirstName, $"%{search}%") ||
                EF.Functions.Like(c.LastName, $"%{search}%") ||
                EF.Functions.Like(c.Email, $"%{search}%"));
        }

        // Se cuenta antes de paginar: el total es el del filtro, no el de la página.
        var totalCount = await query.CountAsync(cancellationToken);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        var items = await query
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResult<Customer>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        TenantCustomers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> GetProfileAsync(int id, CancellationToken cancellationToken = default) =>
        TenantCustomers
            .Include(c => c.Notes.Where(n => n.IsActive).OrderByDescending(n => n.CreatedAt))
            .Include(c => c.Allergies.Where(a => a.IsActive))
            .Include(c => c.Consents.Where(x => x.IsActive))
            // Tres colecciones en una sola consulta multiplicarían las filas.
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        TenantCustomers.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

    public void Add(Customer customer) => _context.Customers.Add(customer);

    public void Update(Customer customer)
    {
        customer.UpdatedAt = DateTime.UtcNow;
        _context.Customers.Update(customer);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
