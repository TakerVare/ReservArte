using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

[ApiController]
[Route("api/v1/auth/external")]
public class ExternalAuthController : ControllerBase
{
   // Mapeo proveedor de la ruta → esquema registrado
    private static readonly Dictionary<string, string> ProviderSchemes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["google"] = "Google",
            ["apple"] = "Apple",
            ["instagram"] = "Instagram",
        };

    private const string ReturnUrlItem = "returnUrl";
    private const string SchemeItem = "scheme";

    private readonly IAuthService _authService;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalAuthController> _logger;

    public ExternalAuthController(
        IAuthService authService,
        ICurrentOrganizationService currentOrganization,
        IAuthenticationSchemeProvider schemeProvider,
        IConfiguration configuration,
        ILogger<ExternalAuthController> logger)
    {
        _authService = authService;
        _currentOrganization = currentOrganization;
        _schemeProvider = schemeProvider;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Inicia el flujo OAuth/OIDC con el proveedor (302 al IdP). El returnUrl
    /// debe pertenecer a un origen permitido (anti open-redirect) y viaja
    /// protegido dentro del state del handler.
    /// </summary>
    [HttpGet("{provider}/challenge")]
    public async Task<IActionResult> ChallengeProvider(string provider, [FromQuery] string returnUrl)
    {
        if (!ProviderSchemes.TryGetValue(provider, out var scheme))
        {
            return BadRequest(ApiResponse.Fail(
                ErrorCodes.GenValidationFailed,
                $"Proveedor '{provider}' no soportado.",
                details: null,
                meta: Meta));
        }

        if (await _schemeProvider.GetSchemeAsync(scheme) is null)
        {
            // Proveedor implementado pero sin credenciales en este entorno
            // (registro condicional de ExternalAuthExtensions)
            return BadRequest(ApiResponse.Fail(
                ErrorCodes.GenValidationFailed,
                $"El proveedor '{provider}' no está configurado en este entorno.",
                details: null,
                meta: Meta));
        }

        if (!IsAllowedReturnUrl(returnUrl))
        {
            return BadRequest(ApiResponse.Fail(
                ErrorCodes.GenValidationFailed,
                "El returnUrl no pertenece a un origen permitido.",
                details: null,
                meta: Meta));
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/v1/auth/external/callback",
        };
        properties.Items[ReturnUrlItem] = returnUrl;
        properties.Items[SchemeItem] = scheme;

        return Challenge(properties, scheme);
    }

    /// <summary>
    /// Retorno tras el IdP: lee el principal externo de la cookie efímera,
    /// resuelve/crea/vincula el usuario y redirige a la SPA con los tokens
    /// en el fragmento de URL (no llega a logs de servidor).
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

        string? returnUrl = null;
        authResult.Properties?.Items.TryGetValue(ReturnUrlItem, out returnUrl);
        var fallbackReturn = $"{FirstAllowedOrigin()}/auth/callback";
        var target = IsAllowedReturnUrl(returnUrl) ? returnUrl! : fallbackReturn;

        if (!authResult.Succeeded || authResult.Principal is null)
        {
            _logger.LogWarning("Callback externo sin autenticación válida");
            return Redirect($"{target}#error=external_auth_failed");
        }

        authResult.Properties!.Items.TryGetValue(SchemeItem, out var schemeItem);
        var provider = schemeItem ?? "unknown";
        var providerKey = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
        var firstName = authResult.Principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = authResult.Principal.FindFirstValue(ClaimTypes.Surname);

        // La cookie externa es de un solo uso: se limpia pase lo que pase
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        if (string.IsNullOrWhiteSpace(providerKey))
        {
            return Redirect($"{target}#error=external_auth_failed");
        }

        var result = await _authService.ExternalLoginAsync(
            provider,
            providerKey,
            email,
            firstName,
            lastName,
            _currentOrganization.OrganizationId!.Value,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!result.Success)
        {
            return Redirect($"{target}#error={Uri.EscapeDataString(result.ErrorCode!)}");
        }

        var fragment =
            $"#access_token={Uri.EscapeDataString(result.Data!.AccessToken)}" +
            $"&refresh_token={Uri.EscapeDataString(result.Data.RefreshToken)}";

        return Redirect($"{target}{fragment}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private ApiMeta Meta => ApiMeta.Create(HttpContext.TraceIdentifier);

    private string[] AllowedOrigins =>
        _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    private string FirstAllowedOrigin() =>
        AllowedOrigins.FirstOrDefault() ?? "http://localhost:3000";

    private bool IsAllowedReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) ||
            !Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var origin = $"{uri.Scheme}://{uri.Authority}";

        return AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }
}