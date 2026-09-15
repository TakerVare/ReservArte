using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación EF Core de <see cref="IEmployeeRepository"/>.
///
/// El aislamiento por tenant de las tablas de disponibilidad lo garantiza el
/// query filter global del contexto; para `Employees`, que todavía no lo tiene,
/// este repositorio filtra explícitamente por la organización actual (ver nota
/// en el PR de RA-869d7ezv0).
/// </summary>
public class EmployeeRepository : IEmployeeRepository
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationService _currentOrganization;

    public EmployeeRepository(
        AppDbContext context, ICurrentOrganizationService currentOrganization)
    {
        _context = context;
        _currentOrganization = currentOrganization;
    }

    /// <summary>
    /// Empleados del tenant actual. El query filter global ya acota por
    /// organización (RA-869f17vet); este filtro explícito se mantiene a
    /// propósito por lo que el global NO cubre: sin organización resuelta, el
    /// global deja pasar todo (lo necesitan migraciones y seeders), y aquí es
    /// preferible una lista vacía a exponer todas las organizaciones. Además
    /// toma el tenant de su propio holder, así que no depende de que el
    /// contexto se haya construido con él.
    /// </summary>
    private IQueryable<Employee> TenantEmployees =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.Employees.Where(e => e.OrganizationId == organizationId)
            : _context.Employees.Where(_ => false);

    public async Task<PagedResult<Employee>> GetPagedAsync(
        EmployeeFilter filter, CancellationToken cancellationToken = default)
    {
        var query = TenantEmployees;

        // Null = solo activos: la baja es lógica y la lista de gestión no debe
        // arrastrar bajas salvo que se pidan explícitamente.
        query = query.Where(e => e.IsActive == (filter.IsActive ?? true));

        if (!string.IsNullOrWhiteSpace(filter.Rol))
        {
            query = query.Where(e => e.Rol == filter.Rol);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(e =>
                EF.Functions.Like(e.FirstName, $"%{search}%") ||
                EF.Functions.Like(e.LastName, $"%{search}%") ||
                EF.Functions.Like(e.Email, $"%{search}%"));
        }

        // Se cuenta antes de paginar: el total es el del filtro, no el de la página.
        var totalCount = await query.CountAsync(cancellationToken);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        var items = await query
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResult<Employee>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        TenantEmployees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Employee?> GetByIdWithAvailabilityAsync(
        int id, CancellationToken cancellationToken = default) =>
        TenantEmployees
            .Include(e => e.Availabilities)
            .Include(e => e.Exceptions)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <summary>
    /// El email es único dentro de la organización, con índice único
    /// (OrganizationId, Email) (RA-869f1xc0u): se busca solo en el tenant
    /// actual, porque la misma persona puede ser empleada en otro centro.
    /// </summary>
    public Task<bool> EmailExistsAsync(
        string email, int? excludeEmployeeId = null, CancellationToken cancellationToken = default) =>
        TenantEmployees
            .Where(e => e.Email == email)
            .Where(e => excludeEmployeeId == null || e.Id != excludeEmployeeId)
            .AnyAsync(cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        TenantEmployees.AnyAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EmployeeAvailability>> GetAvailabilitiesAsync(
        int employeeId, CancellationToken cancellationToken = default) =>
        await _context.EmployeeAvailabilities
            .Where(a => a.EmployeeId == employeeId && a.IsActive)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Ausencias que solapan el intervalo pedido: se incluye toda ausencia que
    /// empiece antes del fin y acabe después del inicio, de modo que también
    /// entran las que envuelven el intervalo por completo.
    /// </summary>
    public async Task<IReadOnlyList<EmployeeException>> GetExceptionsAsync(
        int employeeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default) =>
        await _context.EmployeeExceptions
            .Where(e => e.EmployeeId == employeeId
                        && e.IsActive
                        && e.StartDateTime < to
                        && e.EndDateTime > from)
            .OrderBy(e => e.StartDateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Ausencia concreta de un empleado. El query filter global acota al
    /// tenant; el `employeeId` se exige además para que una ausencia de otro
    /// empleado no se pueda tocar desde la ruta de este.
    /// </summary>
    public Task<EmployeeException?> GetExceptionAsync(
        int employeeId, int exceptionId, CancellationToken cancellationToken = default) =>
        _context.EmployeeExceptions
            .FirstOrDefaultAsync(
                e => e.Id == exceptionId && e.EmployeeId == employeeId, cancellationToken);

    public void AddException(EmployeeException exception) =>
        _context.EmployeeExceptions.Add(exception);

    public void UpdateException(EmployeeException exception)
    {
        exception.UpdatedAt = DateTime.UtcNow;
        _context.EmployeeExceptions.Update(exception);
    }

    public void Add(Employee employee) => _context.Employees.Add(employee);

    public void Update(Employee employee)
    {
        employee.UpdatedAt = DateTime.UtcNow;
        _context.Employees.Update(employee);
    }

    public async Task ReplaceAvailabilitiesAsync(
        int employeeId,
        IEnumerable<EmployeeAvailability> availabilities,
        CancellationToken cancellationToken = default)
    {
        var current = await _context.EmployeeAvailabilities
            .Where(a => a.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        _context.EmployeeAvailabilities.RemoveRange(current);

        foreach (var availability in availabilities)
        {
            // Ni el EmployeeId ni el tenant se toman de la entrada: se imponen
            // aquí para que una petición no pueda colar filas de otro empleado
            // ni de otra organización.
            availability.EmployeeId = employeeId;
            availability.OrganizationId =
                _currentOrganization.OrganizationId ?? availability.OrganizationId;

            _context.EmployeeAvailabilities.Add(availability);
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
