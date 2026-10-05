namespace ReservArte.Application.DTOs.Organizations;

/// <summary>Configuración del centro (RA-869f74u7y).</summary>
public class OrganizationSettingsDto
{
    /// <summary>Zona horaria del centro, identificador IANA.</summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>Horas de antelación por debajo de las cuales una cancelación es tardía.</summary>
    public int CancellationHoursThreshold { get; set; }

    /// <summary>No presentaciones que bloquean a una clienta.</summary>
    public int MaxNoShowsBeforeBlock { get; set; }

    /// <summary>Último guardado; null si el centro sigue con los valores por defecto o no se ha editado.</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Cuerpo de <c>PUT /api/v1/organization/settings</c>: reemplaza la configuración
/// entera. Los números son anulables para distinguir «no viene» de 0, que en el
/// umbral de cancelación es un valor válido.
/// </summary>
public class UpdateOrganizationSettingsRequest
{
    public string? TimeZone { get; set; }

    public int? CancellationHoursThreshold { get; set; }

    public int? MaxNoShowsBeforeBlock { get; set; }
}
