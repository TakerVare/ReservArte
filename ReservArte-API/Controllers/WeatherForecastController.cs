using Microsoft.AspNetCore.Mvc;
using ReservArte.Shared.Api;

namespace ReservArte_API.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Endpoint de plantilla (se eliminará con la tarea del /health).
    /// Sirve como ejemplo del contrato de respuesta con envelope.
    /// </summary>
    [HttpGet(Name = "GetWeatherForecast")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<WeatherForecast>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IEnumerable<WeatherForecast>>> Get()
    {
        var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();

        return Ok(ApiResponse.Ok(
            (IEnumerable<WeatherForecast>)forecast,
            ApiMeta.Create(HttpContext.TraceIdentifier)));
    }
}