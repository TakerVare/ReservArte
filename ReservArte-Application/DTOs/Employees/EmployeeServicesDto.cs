namespace ReservArte.Application.DTOs.Employees;

/// <summary>Servicio que presta un empleado (tabla puente `EmployeeServices`).</summary>
public class EmployeeServiceDto
{
    public int ServiceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }

    /// <summary>Nivel de destreza, de 1 a 5. No es la tarifa.</summary>
    public int ProficiencyLevel { get; init; }

    /// <summary>
    /// El servicio sigue en el catálogo. Una asignación a un servicio retirado
    /// se conserva, pero no cuenta para la reserva.
    /// </summary>
    public bool ServiceIsActive { get; init; }
}

/// <summary>Servicios que presta un empleado, ordenados por nombre.</summary>
public class EmployeeServicesResponse
{
    public int EmployeeId { get; init; }

    public IReadOnlyList<EmployeeServiceDto> Services { get; init; } =
        Array.Empty<EmployeeServiceDto>();
}

/// <summary>
/// Servicios que presta un empleado (PUT .../services). Como el horario, es un
/// reemplazo del conjunto entero: los que no vienen dejan de prestarse (baja
/// lógica) y una lista vacía lo deja sin servicios. Los repetidos cuentan una vez.
/// </summary>
public class UpdateEmployeeServicesRequest
{
    public IReadOnlyList<int> ServiceIds { get; init; } = Array.Empty<int>();
}
