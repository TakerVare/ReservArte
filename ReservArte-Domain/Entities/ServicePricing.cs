namespace ReservArte.Domain.Entities;

/// <summary>
/// Tarifa de un servicio para un nivel de empleada (vol. 1 §3.1.4). Sin fila
/// para el nivel se cobra <see cref="Service.BasePrice"/>.
/// </summary>
public class ServicePricing
{
    public int Id { get; set; }

    /// <summary>
    /// Tenant propietario. Redundante con Service.OrganizationId a propósito:
    /// permite el query filter global sin depender de un JOIN (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int ServiceId { get; set; }

    /// <summary>
    /// Nivel al que se aplica la tarifa; valores de <see cref="EmployeeLevels"/>.
    /// El esquema lo restringe con un CHECK generado desde esas constantes.
    /// </summary>
    public string EmployeeLevel { get; set; } = string.Empty;

    /// <summary>Precio final para ese nivel; no es un recargo sobre el base.</summary>
    public decimal Price { get; set; }

    /// <summary>Baja lógica: las tarifas se desactivan, nunca se borran.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Service Service { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}

/// <summary>
/// Niveles de empleada con tarifa propia. No es el rol de la cuenta
/// (<see cref="Roles"/>, PascalCase porque lo impone <c>[Authorize]</c>): una
/// Manager puede cobrar tarifa junior. Tampoco es el
/// <see cref="EmployeeServiceAssignment.ProficiencyLevel"/>, que mide la
/// destreza en un servicio concreto del 1 al 5.
/// </summary>
public static class EmployeeLevels
{
    public const string Junior = "junior";
    public const string Senior = "senior";
    public const string Expert = "expert";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Junior,
        Senior,
        Expert,
    };
}
