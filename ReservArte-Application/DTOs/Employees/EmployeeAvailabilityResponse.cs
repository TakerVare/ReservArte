namespace ReservArte.Application.DTOs.Employees;

/// <summary>
/// Disponibilidad de un empleado: el horario semanal recurrente y las
/// ausencias que solapan el rango consultado. La disponibilidad real se
/// calcula restando las segundas al primero (contrato del vol. 2 §9.6); esta
/// respuesta entrega las dos piezas en una sola llamada porque la ficha del
/// empleado siempre necesita ambas.
/// </summary>
public class EmployeeAvailabilityResponse
{
    public int EmployeeId { get; init; }

    /// <summary>Tramos recurrentes, ordenados por día y hora (0 = lunes … 6 = domingo).</summary>
    public IReadOnlyList<EmployeeAvailabilityDto> WeeklySchedule { get; init; } =
        Array.Empty<EmployeeAvailabilityDto>();

    public IReadOnlyList<EmployeeExceptionDto> Exceptions { get; init; } =
        Array.Empty<EmployeeExceptionDto>();

    /// <summary>Inicio del rango REALMENTE aplicado a las ausencias (no el pedido).</summary>
    public DateTime ExceptionsFrom { get; init; }

    /// <summary>Fin del rango realmente aplicado a las ausencias.</summary>
    public DateTime ExceptionsTo { get; init; }
}
