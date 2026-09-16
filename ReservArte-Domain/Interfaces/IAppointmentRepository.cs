using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;

namespace ReservArte.Domain.Interfaces;

/// <summary>
/// Criterios de búsqueda de la agenda. Todos opcionales: sin ninguno, devuelve
/// las citas activas del tenant actual.
/// </summary>
public class AppointmentFilter
{
    /// <summary>Desde esta fecha, incluida. Null = sin límite por abajo.</summary>
    public DateOnly? From { get; init; }

    /// <summary>Hasta esta fecha, incluida. Null = sin límite por arriba.</summary>
    public DateOnly? To { get; init; }

    /// <summary>Agenda de una empleada concreta. Null = todas.</summary>
    public int? EmployeeId { get; init; }

    /// <summary>Citas de una clienta concreta. Null = todas.</summary>
    public int? CustomerId { get; init; }

    /// <summary>
    /// Valor de <see cref="AppointmentStatuses"/>. Null = cualquiera. Ojo al
    /// filtrar cancelaciones: son **tres** valores distintos y están juntos en
    /// <see cref="AppointmentStatuses.Cancellations"/>.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Null devuelve solo las activas: la baja es lógica y la agenda de gestión
    /// no debe arrastrar bajas salvo que se pidan. Mismo criterio que
    /// <see cref="CustomerFilter"/> y <see cref="ServiceFilter"/>.
    ///
    /// No confundir con cancelar: cancelar es una transición de
    /// <see cref="Appointment.Status"/> que la clienta ve, y esas citas siguen
    /// siendo activas.
    /// </summary>
    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Acceso a datos de citas (RA-869d7f4n4). Todas las operaciones quedan
/// acotadas al tenant actual; sin organización resuelta no devuelven nada.
///
/// Ningún método recibe la organización por parámetro, a propósito: el tenant
/// sale de <c>ICurrentOrganizationService</c>, igual que en el resto de
/// repositorios. Si se pudiera pasar por argumento, una llamada podría leer la
/// agenda de otro centro.
/// </summary>
public interface IAppointmentRepository
{
    /// <summary>
    /// Lista paginada con filtros, de la más reciente a la más antigua, con su
    /// clienta y su empleada cargadas para que la lista pueda mostrar nombres
    /// sin una consulta por fila.
    /// </summary>
    Task<PagedResult<Appointment>> GetPagedAsync(
        AppointmentFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cita del tenant actual, o null si no existe. **Con seguimiento**: es la
    /// que se lee para modificarla.
    /// </summary>
    Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cita con sus líneas de servicio (en su orden de prestación, con servicio
    /// y variación), su clienta y su empleada. Solo lectura.
    /// </summary>
    Task<Appointment?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Citas del rango de fechas, ambas incluidas, ordenadas por fecha y hora de
    /// inicio. Es la consulta de la agenda y la base para detectar solapes
    /// (RA-869d7f4rd), por lo que **no filtra por estado**: quien la llama
    /// decide si las canceladas ocupan hueco o no.
    /// </summary>
    Task<IReadOnlyList<Appointment>> GetByDateRangeAsync(
        DateOnly from,
        DateOnly to,
        int? employeeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cita por número de pedido de Redsys, o null. **Con seguimiento**: la
    /// respuesta de la pasarela llega para cambiar el estado de la cita
    /// (RA-869d7eden). El número es único dentro del centro.
    /// </summary>
    Task<Appointment?> GetByRedsysOrderAsync(
        string redsysOrderNumber, CancellationToken cancellationToken = default);

    void Add(Appointment appointment);

    void Update(Appointment appointment);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
