namespace ReservArte.Application.DTOs.Customers;

/// <summary>
/// Da o retira un consentimiento (PUT .../consents/{consentType}). Retirar el de
/// tratamiento de datos da de baja la ficha (H-47).
/// </summary>
public class UpdateConsentRequest
{
    public bool Granted { get; init; }
}

/// <summary>Alta o edición de una alergia conocida de la clienta.</summary>
public class CustomerAllergyRequest
{
    public string AllergyDescription { get; init; } = string.Empty;

    /// <summary>Valor de `AllergySeverities`.</summary>
    public string Severity { get; init; } = string.Empty;
}

/// <summary>Bloqueo de la clienta: con él no puede reservar (`CUST_BLOCKED`).</summary>
public class BlockCustomerRequest
{
    public string Reason { get; init; } = string.Empty;
}
