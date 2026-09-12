namespace ReservArte.Domain.Entities;

/// <summary>
/// Tramo horario recurrente de un empleado dentro de la semana. La
/// disponibilidad real se calcula restando a estos tramos las
/// <see cref="EmployeeException"/> que solapen.
/// </summary>
public class EmployeeAvailability
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }

    /// <summary>
    /// Día de la semana en la convención de <see cref="System.DayOfWeek"/>:
    /// 0 = domingo … 6 = sábado (los datos seed usan 1 = lunes). Así
    /// `fecha.DayOfWeek` compara directamente, sin conversión.
    /// </summary>
    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsRecurring { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
}
