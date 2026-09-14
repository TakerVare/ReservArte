namespace ReservArte.Domain.Entities;

/// <summary>
/// Consentimiento RGPD granular de un cliente para una finalidad concreta
/// (vol. 1 §6.1.3, nivel b). No sustituye al consentimiento base de alta
/// (términos y privacidad), que vive en <see cref="User"/>.
/// </summary>
public class CustomerConsent
{
    public int Id { get; set; }

    /// <summary>
    /// Tenant propietario. Redundante con Customer.OrganizationId a propósito:
    /// permite el query filter global sin depender de un JOIN (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int CustomerId { get; set; }

    /// <summary>Finalidad; valores de <see cref="CustomerConsentTypes"/>.</summary>
    public string ConsentType { get; set; } = string.Empty;

    /// <summary>
    /// Estado vigente. <see cref="GrantedAt"/> y <see cref="RevokedAt"/> son la
    /// prueba de cuándo se otorgó y se retiró, que el RGPD exige poder demostrar.
    /// </summary>
    public bool IsGranted { get; set; }

    public DateTime? GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}

/// <summary>Finalidades de <see cref="CustomerConsent.ConsentType"/>.</summary>
public static class CustomerConsentTypes
{
    /// <summary>Tratamiento de datos para gestionar citas. Obligatorio.</summary>
    public const string DataProcessing = "data_processing";

    public const string Marketing = "marketing";

    /// <summary>Fotografías antes/después de los servicios.</summary>
    public const string Photos = "photos";

    /// <summary>Recordatorios de citas por WhatsApp.</summary>
    public const string WhatsApp = "whatsapp";

    /// <summary>Guardar la tarjeta tokenizada por Redsys para pagos futuros.</summary>
    public const string SavedCards = "saved_cards";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        DataProcessing,
        Marketing,
        Photos,
        WhatsApp,
        SavedCards,
    };

    /// <summary>
    /// Finalidades sin las que no se puede dar de alta a un cliente. El resto
    /// son opcionales y nunca se marcan por defecto.
    /// </summary>
    public static readonly IReadOnlyCollection<string> Required = new[]
    {
        DataProcessing,
    };
}
