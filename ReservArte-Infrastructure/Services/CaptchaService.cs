using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReservArte.Application.Interfaces;
using ReservArte.Infrastructure.Options;
using System.Net.Http.Json;

namespace ReservArte.Infrastructure.Services;

public class CaptchaService : ICaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly CaptchaOptions _options;
    private readonly ILogger<CaptchaService> _logger;

    public CaptchaService(
        HttpClient httpClient,
        IOptions<CaptchaOptions> options,
        ILogger<CaptchaService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string? token, string? remoteIp)
    {
        // Dev / entorno sin CAPTCHA: se omite la verificación
        if (!_options.Enabled)
        {
            return true;
        }

        // Con CAPTCHA activo, la ausencia de token es un fallo
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var form = new List<KeyValuePair<string, string>>
            {
                new("secret", _options.SecretKey),
                new("response", token),
            };

            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                form.Add(new("remoteip", remoteIp));
            }

            using var content = new FormUrlEncodedContent(form);
            var response = await _httpClient.PostAsync(_options.VerifyUrl, content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CaptchaVerifyResponse>();

            if (result is null || !result.Success)
            {
                _logger.LogWarning("Verificación de CAPTCHA fallida");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            // Fallo de red o del proveedor: se rechaza por seguridad (fail-closed).
            // Se registra para diagnóstico, sin filtrar el token.
            _logger.LogError(ex, "Error al verificar el CAPTCHA con el proveedor");
            return false;
        }
    }

    // Respuesta del proveedor (Turnstile y reCAPTCHA comparten el campo "success")
    private sealed class CaptchaVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}