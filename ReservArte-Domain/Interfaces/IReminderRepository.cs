using ReservArte.Domain.Entities;

namespace ReservArte.Domain.Interfaces;

/// <summary>
/// Recordatorios del centro de la petición o del job (RA-869d7f5zq). Como el resto
/// de repositorios, no recibe la organización por parámetro.
/// </summary>
public interface IReminderRepository
{
    /// <summary>Recordatorios vigentes del centro, por orden. Sin seguimiento.</summary>
    Task<IReadOnlyList<ReminderConfiguration>> GetActiveConfigurationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Un recordatorio del centro, vigente o no. Sin seguimiento.</summary>
    Task<ReminderConfiguration?> GetConfigurationAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Los avisos ya registrados de una cita. Con seguimiento.</summary>
    Task<IReadOnlyList<ReminderLog>> GetLogsForAppointmentAsync(
        int appointmentId, CancellationToken cancellationToken = default);

    /// <summary>El aviso de una cita, recordatorio y canal, si existe. Con seguimiento.</summary>
    Task<ReminderLog?> GetLogAsync(
        int appointmentId, int reminderConfigurationId, string channel, CancellationToken cancellationToken = default);

    /// <summary>Alta de un aviso: el repositorio impone la organización.</summary>
    void AddLog(ReminderLog log);

    void RemoveLog(ReminderLog log);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
