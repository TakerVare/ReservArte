using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Persistence.Repositories;

/// <summary>Recordatorios del centro actual (RA-869d7f5zq).</summary>
public class ReminderRepository : IReminderRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationService _currentOrganization;

    public ReminderRepository(AppDbContext context, ICurrentOrganizationService currentOrganization)
    {
        _context = context;
        _currentOrganization = currentOrganization;
    }

    public async Task<IReadOnlyList<ReminderConfiguration>> GetActiveConfigurationsAsync(
        CancellationToken cancellationToken = default) =>
        await Configurations().AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.ReminderOrder)
            .ToListAsync(cancellationToken);

    public Task<ReminderConfiguration?> GetConfigurationAsync(
        int id, CancellationToken cancellationToken = default) =>
        Configurations().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ReminderLog>> GetLogsForAppointmentAsync(
        int appointmentId, CancellationToken cancellationToken = default) =>
        await Logs().Where(l => l.AppointmentId == appointmentId).ToListAsync(cancellationToken);

    public Task<ReminderLog?> GetLogAsync(
        int appointmentId, int reminderConfigurationId, string channel, CancellationToken cancellationToken = default) =>
        Logs().FirstOrDefaultAsync(
            l => l.AppointmentId == appointmentId
                && l.ReminderConfigurationId == reminderConfigurationId
                && l.Channel == channel,
            cancellationToken);

    public void AddLog(ReminderLog log)
    {
        log.OrganizationId = _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException("No se puede registrar un aviso sin organización resuelta.");

        _context.ReminderLogs.Add(log);
    }

    public void RemoveLog(ReminderLog log) => _context.ReminderLogs.Remove(log);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    // Segunda barrera además del query filter: sin tenant, ninguna fila.
    private IQueryable<ReminderConfiguration> Configurations() =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.ReminderConfigurations.Where(r => r.OrganizationId == organizationId)
            : _context.ReminderConfigurations.Where(_ => false);

    private IQueryable<ReminderLog> Logs() =>
        _currentOrganization.OrganizationId is { } organizationId
            ? _context.ReminderLogs.Where(l => l.OrganizationId == organizationId)
            : _context.ReminderLogs.Where(_ => false);
}
