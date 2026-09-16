namespace ReservArte.Application.DTOs.Services;

/// <summary>
/// Edición de un servicio. No lleva `IsActive`: la baja y la reactivación son
/// operaciones propias, como en Empleados y Clientes.
/// </summary>
public class UpdateServiceRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DurationMinutes { get; init; }
    public decimal BasePrice { get; init; }
    public int? CategoryId { get; init; }
    public string? ImageUrl { get; init; }
    public bool RequiresAllergyTest { get; init; }
    public int AllergyTestHoursBefore { get; init; } = 48;
}
