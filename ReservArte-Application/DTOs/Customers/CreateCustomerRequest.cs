using ReservArte.Domain.Entities;

namespace ReservArte.Application.DTOs.Customers;

/// <summary>
/// Alta de cliente desde el centro. Sin contraseña, como el alta de empleado:
/// si la cuenta es nueva, la clienta recibe una invitación para crearla.
/// </summary>
public class CreateCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? ProfileImageUrl { get; init; }

    /// <summary>Valor de `CustomerCategories`. Null = `new`, la categoría con la que nace toda ficha.</summary>
    public string? Category { get; init; }

    /// <summary>Valor de `CustomerContactMethods`.</summary>
    public string PreferredContactMethod { get; init; } = CustomerContactMethods.Email;

    /// <summary>
    /// Finalidades que la clienta ha aceptado (valores de `CustomerConsentTypes`).
    /// Debe incluir las obligatorias (`data_processing`); las que no figuran no
    /// se registran, nunca se dan por otorgadas.
    /// </summary>
    public IReadOnlyList<string> GrantedConsents { get; init; } = [];
}
