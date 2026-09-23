namespace ReservArte.Application.DTOs.Appointments;

/// <summary>
/// Cita tal como la devuelven las transiciones de estado (RA-869d7f4xf).
///
/// Lleva los datos de la propia cita, sin los nombres de la clienta ni de la
/// empleada y sin sus líneas de servicio: quien confirma o cancela ya sabe
/// sobre qué cita está actuando. La vista de agenda y el detalle, con sus
/// nombres y su desglose, son de RA-869d7f519, que añadirá lo que necesite.
/// </summary>
public class AppointmentDto
{
    public int Id { get; init; }

    public int CustomerId { get; init; }

    public int EmployeeId { get; init; }

    public DateOnly AppointmentDate { get; init; }

    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }

    /// <summary>Valor de <c>AppointmentStatuses</c>; fuente de verdad del ciclo de vida.</summary>
    public string Status { get; init; } = string.Empty;

    public decimal TotalPrice { get; init; }

    public decimal DepositAmount { get; init; }

    public string? CancellationReason { get; init; }

    public DateTime? CancelledAt { get; init; }

    /// <summary>Cuenta que canceló: puede ser la clienta o alguien del personal.</summary>
    public int? CancelledById { get; init; }

    /// <summary>
    /// Valor de <c>AppointmentCancelledByTypes</c>. Siempre coherente con
    /// <see cref="Status"/> en las citas que cancela el servicio.
    /// </summary>
    public string? CancelledByType { get; init; }

    public string? Notes { get; init; }

    /// <summary>
    /// Baja lógica de gestión. **No** es cancelar: una cita cancelada sigue
    /// activa y la clienta la ve en su historial.
    /// </summary>
    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
