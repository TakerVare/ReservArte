using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core de <see cref="IAppointmentRepository"/> (RA-869d7f4n4).</summary>
public class AppointmentRepository : IAppointmentRepository
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationService _currentOrganization;

    public AppointmentRepository(
        AppDbContext context, ICurrentOrganizationService currentOrganization)
    {
        _context = context;
        _currentOrganization = currentOrganization;
    }

    /// <summary>
    /// Citas del tenant actual. El query filter global ya acota por
    /// organización; este filtro explícito cubre lo que el global no: sin
    /// organización resuelta el global deja pasar todo (lo necesitan migraciones
    /// y seeders), y aquí es preferible no devolver nada.
    /// </summary>
    private IQueryable<Appointment> TenantAppointments =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.Appointments.Where(a => a.OrganizationId == organizationId)
            : _context.Appointments.Where(_ => false);

    public async Task<PagedResult<Appointment>> GetPagedAsync(
        AppointmentFilter filter, CancellationToken cancellationToken = default)
    {
        var query = TenantAppointments.Where(a => a.IsActive == (filter.IsActive ?? true));

        if (filter.From is { } from)
        {
            query = query.Where(a => a.AppointmentDate >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(a => a.AppointmentDate <= to);
        }

        if (filter.EmployeeId is { } employeeId)
        {
            query = query.Where(a => a.EmployeeId == employeeId);
        }

        if (filter.CustomerId is { } customerId)
        {
            query = query.Where(a => a.CustomerId == customerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var status = filter.Status.Trim();
            query = query.Where(a => a.Status == status);
        }

        // Se cuenta antes de paginar: el total es el del filtro, no el de la página.
        var totalCount = await query.CountAsync(cancellationToken);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        // De la más reciente a la más antigua: la agenda se mira hacia delante y
        // lo último reservado es lo que más se consulta.
        var items = await query
            .Include(a => a.Customer)
            .Include(a => a.Employee)
            .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.StartTime)
                .ThenBy(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResult<Appointment>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        TenantAppointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Appointment?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        TenantAppointments
            .Include(a => a.ServiceItems.OrderBy(i => i.Order))
                .ThenInclude(i => i.Service)
            .Include(a => a.ServiceItems.OrderBy(i => i.Order))
                .ThenInclude(i => i.ServiceVariation)
            .Include(a => a.Customer)
            .Include(a => a.Employee)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        int? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = TenantAppointments
            .Where(a => a.IsActive && a.AppointmentDate >= from && a.AppointmentDate <= to);

        if (employeeId is { } id)
        {
            query = query.Where(a => a.EmployeeId == id);
        }

        return await query
            .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ThenBy(a => a.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<Appointment?> GetByRedsysOrderAsync(
        string redsysOrderNumber, CancellationToken cancellationToken = default) =>
        TenantAppointments.FirstOrDefaultAsync(
            a => a.RedsysOrderNumber == redsysOrderNumber, cancellationToken);

    public void Add(Appointment appointment) => _context.Appointments.Add(appointment);

    public void Update(Appointment appointment)
    {
        appointment.UpdatedAt = DateTime.UtcNow;
        _context.Appointments.Update(appointment);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
