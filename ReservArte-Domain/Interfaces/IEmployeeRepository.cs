using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;

namespace ReservArte.Domain.Interfaces;

/// <summary>
/// Criterios de búsqueda de la lista de empleados. Todos opcionales: sin
/// ninguno, devuelve los empleados activos del tenant actual.
/// </summary>
public class EmployeeFilter
{
    /// <summary>Busca en nombre, apellidos y email (contiene, sin distinguir mayúsculas).</summary>
    public string? Search { get; init; }

    /// <summary>Rol exacto ('admin', 'employee'…). Null = cualquiera.</summary>
    public string? Rol { get; init; }

    /// <summary>
    /// Null devuelve solo los activos: la baja es lógica y la lista de gestión
    /// no debe arrastrar bajas salvo que se pidan explícitamente.
    /// </summary>
    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Acceso a datos de empleados y de su disponibilidad. Todas las operaciones
/// quedan acotadas al tenant actual por el query filter global del contexto,
/// de modo que las implementaciones no tienen que repetir el filtro por
/// OrganizationId ni pueden olvidarlo.
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>Lista paginada con filtros, ordenada por apellidos y nombre.</summary>
    Task<PagedResult<Employee>> GetPagedAsync(
        EmployeeFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Empleado por id, o null si no existe en el tenant actual.</summary>
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Empleado con sus horarios y ausencias cargados. Separado de
    /// <see cref="GetByIdAsync"/> para no arrastrar las colecciones en las
    /// consultas que solo necesitan la ficha.
    /// </summary>
    Task<Employee?> GetByIdWithAvailabilityAsync(
        int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Comprueba si el email ya está en uso, opcionalmente ignorando un
    /// empleado (para validar en edición sin chocar consigo mismo).
    /// </summary>
    Task<bool> EmailExistsAsync(
        string email, int? excludeEmployeeId = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Horarios semanales recurrentes de un empleado.</summary>
    Task<IReadOnlyList<EmployeeAvailability>> GetAvailabilitiesAsync(
        int employeeId, CancellationToken cancellationToken = default);

    /// <summary>Ausencias que solapan con el intervalo indicado.</summary>
    Task<IReadOnlyList<EmployeeException>> GetExceptionsAsync(
        int employeeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    void Add(Employee employee);

    void Update(Employee employee);

    /// <summary>
    /// Reemplaza el horario semanal completo de un empleado. Sustituir el
    /// conjunto entero, en vez de aplicar altas y bajas sueltas, evita estados
    /// intermedios incoherentes mientras se edita el horario.
    /// </summary>
    Task ReplaceAvailabilitiesAsync(
        int employeeId,
        IEnumerable<EmployeeAvailability> availabilities,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
