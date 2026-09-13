namespace ReservArte.Domain.Entities;

/// <summary>
/// Servicio que una empleada está capacitada para prestar, con su nivel de
/// destreza. Tabla puente `EmployeeServices` (el nombre de la tabla no cambia).
///
/// La clase se llama ...Assignment y no EmployeeService para no colisionar con
/// el servicio de aplicación `Infrastructure.Services.EmployeeService`: tener
/// ambos con el mismo nombre obliga a poner alias en cada fichero que use los
/// dos namespaces (RA-869f17y7n).
/// </summary>
public class EmployeeServiceAssignment
{
    public int EmployeeId { get; set; }
    public int ServiceId { get; set; }

    /// <summary>Nivel de destreza, de 1 a 5 (CHECK en el esquema).</summary>
    public int ProficiencyLevel { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public Employee Employee { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
