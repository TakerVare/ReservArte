namespace ReservArte.Domain.Entities;

/// <summary>
/// Configuración de un centro (RA-869f74u7y, D-19): una fila por organización.
/// Empieza con lo mínimo del piloto; el resto del diseño objetivo (reserva
/// pública, moneda, medios de pago…) llegará con sus módulos.
///
/// Un centro sin fila funciona con los valores por defecto de esta clase: la
/// fila se crea la primera vez que alguien guarda la configuración.
/// </summary>
public class OrganizationSettings
{
    /// <summary>Zona de un centro sin configuración: la de la península.</summary>
    public const string DefaultTimeZone = "Europe/Madrid";

    /// <summary>Los identificadores IANA más largos rondan los 30 caracteres.</summary>
    public const int TimeZoneMaxLength = 64;

    public const int DefaultCancellationHoursThreshold = 24;
    public const int MinCancellationHoursThreshold = 0;

    /// <summary>Treinta días: tope de cordura, no regla de negocio.</summary>
    public const int MaxCancellationHoursThreshold = 720;

    public const int DefaultMaxNoShowsBeforeBlock = 3;
    public const int MinMaxNoShowsBeforeBlock = 1;
    public const int MaxMaxNoShowsBeforeBlock = 99;

    public int Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Zona horaria del centro, identificador IANA (`Europe/Madrid`,
    /// `Atlantic/Canary`). El horario y las citas se guardan en hora local del
    /// centro: esta zona dice qué es «hoy» y «ahora» y cómo se pasan a UTC.
    /// </summary>
    public string TimeZone { get; set; } = DefaultTimeZone;

    /// <summary>
    /// Horas de antelación por debajo de las cuales una cancelación es tardía.
    /// 0 = nunca es tardía.
    /// </summary>
    public int CancellationHoursThreshold { get; set; } = DefaultCancellationHoursThreshold;

    /// <summary>No presentaciones que bloquean a una clienta (H-21; lo aplicará RA-869f2gtyv).</summary>
    public int MaxNoShowsBeforeBlock { get; set; } = DefaultMaxNoShowsBeforeBlock;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
}
