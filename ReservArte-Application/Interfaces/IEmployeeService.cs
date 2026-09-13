using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Domain.Common;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Casos de uso del módulo de Empleados. Todas las operaciones quedan acotadas
/// al tenant de la petición; el servicio nunca lo acepta como parámetro para
/// que una capa superior no pueda suplantarlo.
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
}
