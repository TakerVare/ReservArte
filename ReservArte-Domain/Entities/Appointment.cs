namespace ReservArte.Domain.Entities;

/// <summary>
/// Cita de un cliente con una empleada, en una fecha y una franja concretas.
/// Es el núcleo del producto: de aquí cuelgan los servicios prestados, los
/// pagos y los recordatorios.
///
/// La duración y el importe NO se recalculan al leer: se congelan al crear la
/// cita en <see cref="AppointmentServiceItem"/>, porque el precio de un servicio
/// puede cambiar después y una cita ya cerrada debe seguir valiendo lo que se
/// cobró.
/// </summary>
public class Appointment
{
    public int Id { get; set; }

    public Guid OrganizationId { get; set; }

    public int CustomerId { get; set; }
    public int EmployeeId { get; set; }

    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// Hora de fin, calculada al crear la cita a partir de la duración de sus
    /// servicios. Se guarda para que la agenda pueda detectar solapes sin sumar
    /// las líneas en cada consulta.
    /// </summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// Fase del ciclo de vida; valores de <see cref="AppointmentStatuses"/>. El
    /// esquema lo restringirá con un CHECK generado desde esas constantes.
    /// Máquina de estados en vol. 1 §5.2.2.
    /// </summary>
    public string Status { get; set; } = AppointmentStatuses.Pending;

    /// <summary>Importe total pactado, ya cerrado al crear la cita.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>Señal cobrada por adelantado, si la hay.</summary>
    public decimal DepositAmount { get; set; }

    /// <summary>
    /// Número de pedido de Redsys. Escalar, sin FK: la pasarela llega con
    /// RA-869d7eden. RA-869d7f4j8 le pondrá índice único.
    /// </summary>
    public string? RedsysOrderNumber { get; set; }

    /// <summary>Token de la pre-autorización de Redsys (RA-869d7eden).</summary>
    public string? RedsysPreAuthToken { get; set; }

    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Quién canceló, como Id de cuenta. Escalar sin FK declarada, igual que en
    /// el sketch de vol. 1 §5.2: puede ser la clienta o alguien del personal, y
    /// una FK a <see cref="User"/> obligaría a elegir uno de los dos caminos.
    /// </summary>
    public int? CancelledById { get; set; }

    /// <summary>
    /// Quién canceló, por tipo; valores de
    /// <see cref="AppointmentCancelledByTypes"/>.
    ///
    /// OJO — redundancia conocida: <see cref="Status"/> ya distingue
    /// <c>cancelled_by_customer</c> de <c>cancelled_by_business</c>, así que el
    /// mismo dato vive en dos columnas. **`Status` es la fuente de verdad**; este
    /// campo lo acompaña y debe mantenerse coherente con él. Imponer esa
    /// coherencia al cancelar es trabajo de RA-869d7f4xf (máquina de estados).
    /// </summary>
    public string? CancelledByType { get; set; }

    /// <summary>Notas internas del personal sobre la cita.</summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Baja lógica. No es lo mismo que cancelar: cancelar es una transición de
    /// <see cref="Status"/> que el cliente ve, mientras que esto retira la fila
    /// de las listas de gestión.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Employee Employee { get; set; } = null!;

    /// <summary>Servicios prestados en la cita, en su orden de prestación.</summary>
    public ICollection<AppointmentServiceItem> ServiceItems { get; set; } =
        new List<AppointmentServiceItem>();

    // PaymentMethod, Payments, Photos, ReminderLogs y ConfirmationTokens llegan
    // con sus propios módulos (Redsys, fotografías y recordatorios), no antes:
    // hoy esas entidades no están en el DbContext. Mismo criterio que Customer,
    // Employee y Service. Por eso tampoco está PaymentMethodId: es una FK a
    // CustomerPaymentMethod, que sigue en Ignore (RA-869f2gnbm).
}

/// <summary>
/// Valores admitidos por <see cref="Appointment.Status"/>, fieles al CHECK de
/// diseño de vol. 1 §5.2.2. Constantes y no enum, por coherencia con el resto
/// del dominio, que persiste estos campos como texto.
///
/// La cancelación se desdobla en tres valores (genérica, por la clienta y por el
/// centro) a propósito, siguiendo el diseño escrito. Para no repetir esos tres
/// literales en cada consulta existe <see cref="Cancellations"/>.
/// </summary>
public static class AppointmentStatuses
{
    /// <summary>Pendiente de confirmación o de pago. Estado inicial.</summary>
    public const string Pending = "pending";

    public const string Confirmed = "confirmed";

    /// <summary>En curso: la clienta ha llegado y el servicio ha empezado.</summary>
    public const string InProgress = "in_progress";

    public const string Completed = "completed";

    /// <summary>Cancelada sin distinguir quién; ver <see cref="Cancellations"/>.</summary>
    public const string Cancelled = "cancelled";

    public const string CancelledByCustomer = "cancelled_by_customer";
    public const string CancelledByBusiness = "cancelled_by_business";

    /// <summary>No presentada. Alimenta el contador de no-shows (RA-869f2gtyv).</summary>
    public const string NoShow = "no_show";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Pending,
        Confirmed,
        InProgress,
        Completed,
        Cancelled,
        CancelledByCustomer,
        CancelledByBusiness,
        NoShow,
    };

    /// <summary>
    /// Los tres valores que significan «cancelada». Filtrar cancelaciones exige
    /// mirar los tres, así que se nombran una sola vez aquí en lugar de repetir
    /// los literales en cada consulta.
    /// </summary>
    public static readonly IReadOnlyCollection<string> Cancellations = new[]
    {
        Cancelled,
        CancelledByCustomer,
        CancelledByBusiness,
    };

    /// <summary>
    /// Estados de los que ya no se sale (vol. 1 §5.2.2): desde Completed no se
    /// vuelve a estados abiertos, y reagendar es una cita nueva.
    /// </summary>
    public static readonly IReadOnlyCollection<string> Terminal = new[]
    {
        Completed,
        Cancelled,
        CancelledByCustomer,
        CancelledByBusiness,
        NoShow,
    };
}

/// <summary>
/// Valores admitidos por <see cref="Appointment.CancelledByType"/>. Debe
/// mantenerse coherente con el <see cref="Appointment.Status"/>, que es la
/// fuente de verdad.
/// </summary>
public static class AppointmentCancelledByTypes
{
    public const string Customer = "customer";

    /// <summary>El centro: da igual qué miembro del personal la cancele.</summary>
    public const string Business = "business";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Customer,
        Business,
    };
}
