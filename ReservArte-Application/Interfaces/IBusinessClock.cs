namespace ReservArte.Application.Interfaces;

/// <summary>
/// Zona horaria del centro de la petición (RA-869f74u7y, D-19). El horario y las
/// citas se guardan en hora local del centro, y las ausencias en UTC: todo lo que
/// las compare, o que necesite saber qué es «hoy» y «ahora», pasa por aquí.
/// </summary>
public interface IBusinessClock
{
    /// <summary>
    /// La zona configurada (<c>OrganizationSettings.TimeZone</c>; sin fila, la
    /// zona por defecto), o null si esta máquina no la resuelve. Quien llama
    /// decide qué hacer sin zona; lo habitual es trabajar en UTC.
    /// </summary>
    Task<TimeZoneInfo?> FindTimeZoneAsync(CancellationToken cancellationToken = default);
}
