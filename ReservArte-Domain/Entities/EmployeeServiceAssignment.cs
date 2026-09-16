namespace ReservArte.Domain.Entities;

/// <summary>
/// Servicio que una empleada está capacitada para prestar, con su nivel de
/// destreza. Tabla puente `EmployeeServices` (el nombre de la tabla no cambia).
/// Es la que permite ofrecer solo a quien sabe hacer el servicio al calcular
/// los huecos de la agenda.
///
/// La clase se llama ...Assignment y no EmployeeService para no colisionar con
/// el servicio de aplicación `Infrastructure.Services.EmployeeService`: tener
/// ambos con el mismo nombre obliga a poner alias en cada fichero que use los
/// dos namespaces (RA-869f17y7n).
/// </summary>
public class EmployeeServiceAssignment
{
    /// <summary>
    /// Tenant propietario. Redundante con Employee.OrganizationId y
    /// Service.OrganizationId a propósito: permite el query filter global sin
    /// depender de un JOIN (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int EmployeeId { get; set; }
    public int ServiceId { get; set; }

    /// <summary>
    /// Nivel de destreza en este servicio, de 1 a 5 (CHECK en el esquema). No
    /// es la tarifa: esa la fija <see cref="EmployeeLevels"/> en
    /// <see cref="ServicePricing"/>.
    /// </summary>
    public int ProficiencyLevel { get; set; } = 1;

    /// <summary>Baja lógica: las asignaciones se desactivan, nunca se borran.</summary>
    public bool IsActive { get; set; } = true;

    public Employee Employee { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
