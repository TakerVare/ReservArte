using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReservArte.API.Models;

namespace ReservArte.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Health check de la API: proceso vivo + smoke test de conectividad
    /// a la base de datos vía EF Core. Excepción documentada sin envelope
    /// (vol. 1 §5.1.1). Fuera de /api: no exige tenant (lo consumen ALB
    /// y monitorización). Devuelve 200 si todo está sano y 503 si alguna
    /// comprobación falla.
    /// </summary>
    [HttpGet(Name = "GetHealth")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthCheckResponse>> Get(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);

        var response = HealthCheckResponse.FromReport(report);

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}