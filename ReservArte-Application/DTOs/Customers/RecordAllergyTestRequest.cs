namespace ReservArte.Application.DTOs.Customers;

/// <summary>
/// Registro de la prueba de alergia de una clienta (RA-869f9cu2x). Fecha y hora en
/// ISO 8601 con zona, como toda fecha con hora de la API; no puede ser futura.
/// </summary>
public class RecordAllergyTestRequest
{
    public DateTime TestedAt { get; init; }
}
