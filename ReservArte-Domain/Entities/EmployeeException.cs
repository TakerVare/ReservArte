namespace ReservArte.Domain.Entities;

/// <summary>
/// Ausencia puntual de un empleado en un intervalo concreto: prevalece sobre
/// el horario recurrente de <see cref="EmployeeAvailability"/>.
/// </summary>
public class EmployeeException
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Reason { get; set; }

    /// <summary>
    /// Motivo tipificado. El esquema lo restringe con un CHECK a los valores
    /// de <see cref="EmployeeExceptionTypes"/>.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Employee Employee { get; set; } = null!;
}

/// <summary>
/// Valores admitidos por <see cref="EmployeeException.Type"/>, fieles al CHECK
/// del esquema. Constantes y no enum, por coherencia con el resto del dominio
/// (p. ej. <see cref="Employee.Rol"/>), que persiste estos campos como texto.
/// </summary>
public static class EmployeeExceptionTypes
{
    public const string Vacation = "vacation";
    public const string SickLeave = "sick_leave";
    public const string Personal = "personal";
    public const string Training = "training";
    public const string Other = "other";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Vacation,
        SickLeave,
        Personal,
        Training,
        Other,
    };
}
