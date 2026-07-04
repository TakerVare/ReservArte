using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ReservArte.API.Models;

/// <summary>
/// Respuesta de GET /health. Excepción documentada SIN envelope
/// (vol. 1 §5.1.1): la consumen balanceadores (ALB) y monitorización,
/// no clientes de negocio.
/// </summary>
public class HealthCheckResponse
{
    /// <summary>Estado global: Healthy, Degraded o Unhealthy.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Duración total de todas las comprobaciones en ms.</summary>
    public double TotalDurationMs { get; init; }

    /// <summary>Detalle de cada comprobación registrada.</summary>
    public IReadOnlyList<HealthCheckEntry> Checks { get; init; } = Array.Empty<HealthCheckEntry>();

    public static HealthCheckResponse FromReport(HealthReport report) => new()
    {
        Status = report.Status.ToString(),
        TotalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
        Checks = report.Entries
            .Select(entry => new HealthCheckEntry
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                DurationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
                Description = entry.Value.Description
            })
            .ToList()
    };
}

/// <summary>Resultado individual de una comprobación de salud.</summary>
public class HealthCheckEntry
{
    public string Name { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public double DurationMs { get; init; }

    public string? Description { get; init; }
}