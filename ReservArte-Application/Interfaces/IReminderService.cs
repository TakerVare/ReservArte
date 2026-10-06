namespace ReservArte.Application.Interfaces;

/// <summary>
/// Recordatorios de cita (RA-869d7f5zq). Programa un aviso por cada recordatorio
/// vigente del centro y canal, y decide, cuando le llega la hora, si todavía hay
/// que enviarlo. El envío por canal llega con RA-869d7f61y.
/// </summary>
public interface IReminderService
{
    /// <summary>
    /// Programa los recordatorios de una cita confirmada del centro actual. Se
    /// llama al confirmarla y al cambiarle la fecha o la hora: es idempotente, y
    /// un aviso cuya hora ya pasó no se programa. No lanza: un fallo aquí no debe
    /// deshacer la confirmación, así que se registra y se sigue.
    /// </summary>
    Task ScheduleForAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lo que ejecuta el job cuando llega la hora de un aviso, con el tenant ya
    /// fijado. Comprueba que la cita sigue confirmada, que no se ha movido a más
    /// tarde y que el aviso no se ha tratado ya.
    /// </summary>
    Task<ReminderDueOutcome> ProcessDueAsync(
        int appointmentId, int reminderConfigurationId, string channel, CancellationToken cancellationToken = default);
}

/// <summary>Qué pasó con un aviso al llegarle la hora.</summary>
public static class ReminderDueOutcomes
{
    /// <summary>Hay que enviarlo: su registro sigue pendiente.</summary>
    public const string Ready = "ready";

    /// <summary>La cita o el recordatorio ya no existen, o la cita no está confirmada: se descarta.</summary>
    public const string Discarded = "discarded";

    /// <summary>La cita se movió y a este aviso aún no le toca: lo enviará el job programado después.</summary>
    public const string NotYet = "not_yet";

    /// <summary>Ya se envió o falló: no se repite.</summary>
    public const string AlreadyHandled = "already_handled";
}

/// <summary>Resultado de <see cref="IReminderService.ProcessDueAsync"/>; valores de <see cref="ReminderDueOutcomes"/>.</summary>
public sealed record ReminderDueOutcome(string Value, int? ReminderLogId = null);

/// <summary>Un aviso concreto: de qué centro, cita, recordatorio y canal.</summary>
public sealed record ReminderJobKey(Guid OrganizationId, int AppointmentId, int ReminderConfigurationId, string Channel);

/// <summary>
/// Cola de trabajos en segundo plano para los avisos (RA-869d7f5zq). La
/// implementación real es Hangfire; el servicio no lo conoce, y los tests la
/// sustituyen para ver qué se programó y para cuándo.
/// </summary>
public interface IReminderJobScheduler
{
    /// <summary>Programa el aviso para ese instante (UTC).</summary>
    void Schedule(ReminderJobKey key, DateTimeOffset sendAt);
}
