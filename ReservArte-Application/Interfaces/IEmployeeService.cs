using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Domain.Common;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Casos de uso del módulo de Empleados. Todas las operaciones quedan acotadas
/// al tenant de la petición; el servicio nunca lo acepta como parámetro para
/// que una capa superior no pueda suplantarlo.
///
/// Las operaciones de escritura aplican además reglas según quién llama
/// (GEN_FORBIDDEN): solo un Admin asigna el rol Admin o gestiona a otro Admin,
/// nadie cambia su propio rol y nadie se da de baja a sí mismo.
/// </summary>
public interface IEmployeeService
{
    Task<Result<PagedResult<EmployeeDto>>> GetPagedAsync(
        EmployeeFilter filter, CancellationToken cancellationToken = default);

    Task<Result<EmployeeDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<EmployeeDto>> CreateAsync(
        CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<Result<EmployeeDto>> UpdateAsync(
        int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Baja lógica: el empleado se desactiva, nunca se borra.</summary>
    Task<Result<EmployeeDto>> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Reactiva a un empleado dado de baja.</summary>
    Task<Result<EmployeeDto>> ReactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Horario semanal del empleado y sus ausencias en el rango indicado
    /// (por defecto, desde hoy y 90 días). La consulta no aplica la regla de
    /// «un Manager no toca a un Admin»: la lista de empleados ya muestra a los
    /// Admin, y aquí solo se lee.
    /// </summary>
    Task<Result<EmployeeAvailabilityResponse>> GetAvailabilityAsync(
        int employeeId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reemplaza el horario semanal completo. Devuelve la disponibilidad
    /// resultante, con el rango de ausencias por defecto.
    /// </summary>
    Task<Result<EmployeeAvailabilityResponse>> ReplaceAvailabilityAsync(
        int employeeId,
        UpdateAvailabilityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Registra una ausencia puntual del empleado.</summary>
    Task<Result<EmployeeExceptionDto>> AddExceptionAsync(
        int employeeId,
        CreateEmployeeExceptionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retira una ausencia. Baja lógica, como en la ficha del empleado: deja
    /// de contar para la disponibilidad, pero no se pierde el histórico.
    /// </summary>
    Task<Result<EmployeeExceptionDto>> DeleteExceptionAsync(
        int employeeId,
        int exceptionId,
        CancellationToken cancellationToken = default);
}
