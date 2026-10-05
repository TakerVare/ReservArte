namespace ReservArte.Domain.Entities;

/// <summary>
/// Enlace de un solo uso que viaja en un recordatorio para confirmar o cancelar
/// la cita sin iniciar sesión (RA-869d7f5wx). El token es la clave: quien lo
/// presenta actúa sobre esa cita, una vez y antes de que caduque.
/// </summary>
public class ConfirmationToken
{
    /// <summary>Longitud máxima del token (texto opaco, apto para una URL).</summary>
    public const int TokenMaxLength = 64;

    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Tenant propietario. Redundante con el de la cita a propósito: permite el
    /// query filter global sin depender de un JOIN (RA-869f17myx).
    /// </summary>
    public Guid OrganizationId { get; set; }

    public int AppointmentId { get; set; }

    /// <summary>Qué hace el enlace; valores de <see cref="ConfirmationTokenActions"/>.</summary>
    public string Action { get; set; } = ConfirmationTokenActions.Confirm;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Cuándo se usó (UTC). Con valor, el token ya no sirve.</summary>
    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organization Organization { get; set; } = null!;
    public Appointment Appointment { get; set; } = null!;
}

/// <summary>Valores admitidos por <see cref="ConfirmationToken.Action"/>.</summary>
public static class ConfirmationTokenActions
{
    public const string Confirm = "confirm";
    public const string Cancel = "cancel";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Confirm,
        Cancel,
    };
}
