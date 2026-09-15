using ReservArte.Domain.Entities;

namespace ReservArte.Application.DTOs.Customers;

/// <summary>
/// Edición de la ficha. Los consentimientos, el bloqueo y la baja no se tocan
/// aquí: cada uno tiene su propia operación.
/// </summary>
public class UpdateCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? ProfileImageUrl { get; init; }

    /// <summary>Valor de `CustomerCategories`.</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Valor de `CustomerContactMethods`.</summary>
    public string PreferredContactMethod { get; init; } = CustomerContactMethods.Email;
}
