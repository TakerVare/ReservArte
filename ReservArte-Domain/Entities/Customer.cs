namespace ReservArte.Domain.Entities;

public class Customer
{
    /// <summary>
    /// Clave primaria compartida con <see cref="User"/>, igual que
    /// <see cref="Employee"/>: no hay columna UserId, el Id del cliente ES el de
    /// su cuenta. Todo cliente tiene fila en AspNetUsers, aunque la dé de alta el
    /// centro y aún no tenga contraseña.
    /// </summary>
    public int Id { get; set; }

    public Guid OrganizationId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Obligatorio: es el canal de recordatorios por defecto. La unicidad se
    /// decide con el esquema (RA-869d7f32r): la cuenta de Identity asociada ya
    /// impone hoy un email único global.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateOnly? BirthDate { get; set; }

    /// <summary>Segmento comercial; valores de <see cref="CustomerCategories"/>.</summary>
    public string Category { get; set; } = CustomerCategories.Regular;

    public int LoyaltyPoints { get; set; }

    /// <summary>
    /// Bloqueo para reservar (no-shows reiterados o decisión del centro). No es
    /// una categoría: así un cliente VIP bloqueado conserva su segmento al
    /// desbloquearse.
    /// </summary>
    public bool IsBlocked { get; set; }

    public string? BlockedReason { get; set; }

    /// <summary>Valores de <see cref="CustomerContactMethods"/>.</summary>
    public string PreferredContactMethod { get; set; } = CustomerContactMethods.Email;

    /// <summary>Baja lógica: los clientes se desactivan, nunca se borran.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;

    /// <summary>Notas internas del personal; el cliente no las ve.</summary>
    public ICollection<CustomerNote> Notes { get; set; } = new List<CustomerNote>();

    public ICollection<CustomerAllergy> Allergies { get; set; } = new List<CustomerAllergy>();

    /// <summary>
    /// Consentimientos RGPD granulares: única fuente de verdad (no hay un bool
    /// de marketing en el cliente que pueda divergir de ellos).
    /// </summary>
    public ICollection<CustomerConsent> Consents { get; set; } = new List<CustomerConsent>();

    public ICollection<CustomerPaymentMethod> PaymentMethods { get; set; } =
        new List<CustomerPaymentMethod>();

    // Appointments, Payments y WaitingLists llegan con sus propios módulos
    // (citas, pagos y lista de espera), no antes: hoy esas entidades no están en
    // el DbContext.
}

/// <summary>
/// Valores admitidos por <see cref="Customer.Category"/>. Catálogo fijo, no
/// configurable por organización. «Bloqueado» no está a propósito: es
/// <see cref="Customer.IsBlocked"/>.
/// </summary>
public static class CustomerCategories
{
    public const string Regular = "regular";
    public const string Vip = "vip";
    public const string New = "new";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Regular,
        Vip,
        New,
    };
}

/// <summary>Valores admitidos por <see cref="Customer.PreferredContactMethod"/>.</summary>
public static class CustomerContactMethods
{
    public const string Email = "email";
    public const string Phone = "phone";
    public const string Sms = "sms";
    public const string WhatsApp = "whatsapp";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Email,
        Phone,
        Sms,
        WhatsApp,
    };
}
