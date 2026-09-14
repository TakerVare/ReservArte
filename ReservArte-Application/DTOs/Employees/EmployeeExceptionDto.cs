namespace ReservArte.Application.DTOs.Employees;

/// <summary>
/// Ausencia puntual de un empleado (vacaciones, baja, formación…) tal como la
/// expone la API. Prevalece sobre el horario semanal en su intervalo.
/// </summary>
public class EmployeeExceptionDto
{
    public int Id { get; init; }

    public DateTime StartDateTime { get; init; }

    public DateTime EndDateTime { get; init; }

    /// <summary>Uno de `EmployeeExceptionTypes`.</summary>
    public string Type { get; init; } = string.Empty;

    public string? Reason { get; init; }

    public bool IsActive { get; init; }
}
