namespace ReservArte.Domain.Entities;

/// <summary>
/// Tramo horario recurrente de un empleado dentro de la semana. La
/// disponibilidad real se calcula restando a estos tramos las
/// <see cref="EmployeeException"/> que solapen.
/// </summary>
public class EmployeeAvailability
{
    public int Id { get; set; }

    /// <summary>
    /// Tenant propietario. Redundante con Employee.OrganizationId a propósito:
    /// permite el query filter global sin depender de un JOIN, de modo que una
    /// consulta directa a esta tabla tampoco cruce organizaciones (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int EmployeeId { get; set; }

    /// <summary>
    /// Día de la semana en la convención del proyecto: <b>0 = lunes … 6 = domingo</b>
    /// (semana europea). NO coincide con <see cref="System.DayOfWeek"/>, que
    /// empieza en domingo: al partir de una fecha hay que convertir con
    /// <see cref="WeekDay.FromDate"/>, nunca usar el <c>int</c> de
    /// <c>fecha.DayOfWeek</c> directamente.
    /// </summary>
    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsRecurring { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}

/// <summary>
/// Conversión entre <see cref="System.DayOfWeek"/> (domingo = 0) y la
/// convención del proyecto para <see cref="EmployeeAvailability.DayOfWeek"/>
/// (lunes = 0). Existe para que el desfase se resuelva en un único sitio:
/// hacerlo a mano en cada consulta es una fuente segura de errores de un día.
/// </summary>
public static class WeekDay
{
    public const int Monday = 0;
    public const int Tuesday = 1;
    public const int Wednesday = 2;
    public const int Thursday = 3;
    public const int Friday = 4;
    public const int Saturday = 5;
    public const int Sunday = 6;

    /// <summary>Día del proyecto (lunes = 0) correspondiente a una fecha.</summary>
    public static int FromDate(DateTime date) => FromDayOfWeek(date.DayOfWeek);

    /// <summary>Día del proyecto (lunes = 0) correspondiente a una fecha.</summary>
    public static int FromDate(DateOnly date) => FromDayOfWeek(date.DayOfWeek);

    /// <summary>
    /// Traduce <see cref="System.DayOfWeek"/> (domingo = 0 … sábado = 6) a la
    /// convención del proyecto (lunes = 0 … domingo = 6).
    /// </summary>
    public static int FromDayOfWeek(DayOfWeek dayOfWeek) => ((int)dayOfWeek + 6) % 7;

    /// <summary>Operación inversa de <see cref="FromDayOfWeek"/>.</summary>
    public static DayOfWeek ToDayOfWeek(int projectDay) => (DayOfWeek)((projectDay + 1) % 7);
}
