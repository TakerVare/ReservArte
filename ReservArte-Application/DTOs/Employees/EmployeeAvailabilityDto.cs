namespace ReservArte.Application.DTOs.Employees;

/// <summary>Tramo del horario semanal de un empleado.</summary>
public class EmployeeAvailabilityDto
{
    public int Id { get; init; }

    /// <summary>Convención del proyecto: 0 = lunes … 6 = domingo.</summary>
    public int DayOfWeek { get; init; }

    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public bool IsRecurring { get; init; }
    public bool IsActive { get; init; }
}
