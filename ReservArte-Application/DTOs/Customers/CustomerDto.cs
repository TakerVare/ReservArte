namespace ReservArte.Application.DTOs.Customers;

/// <summary>Ficha de cliente tal como la expone la API en listados y ediciones.</summary>
public class CustomerDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;

    /// <summary>Nombre y apellidos, para listados y selectores.</summary>
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? ProfileImageUrl { get; init; }
    public DateOnly? BirthDate { get; init; }

    /// <summary>Valor de `CustomerCategories`.</summary>
    public string Category { get; init; } = string.Empty;

    public int LoyaltyPoints { get; init; }
    public bool IsBlocked { get; init; }
    public string? BlockedReason { get; init; }

    /// <summary>Valor de `CustomerContactMethods`.</summary>
    public string PreferredContactMethod { get; init; } = string.Empty;

    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // OrganizationId NO se expone, mismo criterio que EmployeeDto: el tenant lo
    // resuelve el servidor.
}

/// <summary>
/// Perfil completo del cliente: la ficha con sus consentimientos, alergias y
/// notas vigentes. El historial de citas llegará con el módulo de Citas.
/// </summary>
public class CustomerDetailDto : CustomerDto
{
    public IReadOnlyList<CustomerConsentDto> Consents { get; init; } = [];
    public IReadOnlyList<CustomerAllergyDto> Allergies { get; init; } = [];

    /// <summary>Notas internas del personal, las más recientes primero.</summary>
    public IReadOnlyList<CustomerNoteDto> Notes { get; init; } = [];
}

public class CustomerConsentDto
{
    /// <summary>Valor de `CustomerConsentTypes`.</summary>
    public string ConsentType { get; init; } = string.Empty;

    public bool IsGranted { get; init; }
    public DateTime? GrantedAt { get; init; }
    public DateTime? RevokedAt { get; init; }
}

public class CustomerAllergyDto
{
    public int Id { get; init; }
    public string AllergyDescription { get; init; } = string.Empty;

    /// <summary>Valor de `AllergySeverities`.</summary>
    public string Severity { get; init; } = string.Empty;
}

public class CustomerNoteDto
{
    public int Id { get; init; }
    public string Note { get; init; } = string.Empty;

    /// <summary>Autor de la nota.</summary>
    public int EmployeeId { get; init; }

    public DateTime CreatedAt { get; init; }
}
