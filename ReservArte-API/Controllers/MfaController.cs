using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Domain.Entities;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

/// <summary>
/// Gestión del segundo factor TOTP (vol. 1 §4.4.2) para el usuario
/// autenticado: enable (genera secreto + URI otpauth), confirm (verifica
/// el primer código y activa) y disable (verifica y desactiva). El QR se
/// entrega como URI otpauth:// que el frontend renderiza; el backend no
/// genera imagen. El estado intermedio mfa_required en el login pertenece
/// a la tarea siguiente (RA-869d7ezgy).
/// </summary>
[ApiController]
[Route("api/v1/account/mfa")]
[Authorize]
public class MfaController : ControllerBase
{
    // Etiqueta del emisor que se muestra en la app autenticadora
    private const string Issuer = "ReservArte";

    private readonly UserManager<User> _userManager;
    private readonly ILogger<MfaController> _logger;

    public MfaController(UserManager<User> userManager, ILogger<MfaController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    private ApiMeta Meta => ApiMeta.Create(HttpContext.TraceIdentifier);

    /// <summary>
    /// Genera (o regenera) el secreto TOTP y devuelve la URI otpauth para
    /// el QR más la clave en texto para entrada manual. NO activa el 2FA
    /// todavía: eso ocurre en confirm, tras verificar el primer código.
    /// </summary>
    [HttpPost("enable")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enable()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized(ApiResponse.Fail(
                ErrorCodes.AuthInvalidCredentials, "Sesión no válida.", null, Meta));
        }

        if (user.TwoFactorEnabled)
        {
            return Conflict(ApiResponse.Fail(
                ErrorCodes.GenConflict,
                "El doble factor ya está activado. Desactívalo antes de regenerarlo.",
                null, Meta));
        }

        // Genera una clave nueva (descarta cualquier secreto no confirmado
        // de un enable anterior) y la lee para construir la URI
        await _userManager.ResetAuthenticatorKeyAsync(user);
        var key = await _userManager.GetAuthenticatorKeyAsync(user);

        var email = user.Email ?? user.UserName ?? "usuario";
        var otpauthUri = BuildOtpauthUri(email, key!);

        _logger.LogInformation("Secreto TOTP generado para el usuario {UserId}", user.Id);

        return Ok(ApiResponse.Ok<object>(
            new
            {
                otpauthUri,
                manualEntryKey = FormatKey(key!),
            },
            Meta));
    }

    /// <summary>
    /// Verifica el primer código de 6 dígitos generado por la app y, si es
    /// correcto, activa el 2FA para la cuenta.
    /// </summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Confirm([FromBody] MfaCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
        {
            return BadRequest(ApiResponse.Fail(
                ErrorCodes.GenValidationFailed, "El código es obligatorio.", null, Meta));
        }

        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized(ApiResponse.Fail(
                ErrorCodes.AuthInvalidCredentials, "Sesión no válida.", null, Meta));
        }

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            request.Code.Replace(" ", string.Empty));

        if (!isValid)
        {
            return BadRequest(ApiResponse.Fail(
                ErrorCodes.AuthInvalidCredentials,
                "El código no es válido. Revisa la hora del dispositivo e inténtalo de nuevo.",
                null, Meta));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        // Códigos de recuperación de un solo uso: la ÚNICA vez que se
        // muestran es aquí. El usuario debe guardarlos; sirven para entrar
        // si pierde el dispositivo TOTP (se canjean en /auth/mfa/verify).
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        _logger.LogInformation("2FA activado para el usuario {UserId}", user.Id);

        return Ok(ApiResponse.Ok<object>(
            new
            {
                twoFactorEnabled = true,
                recoveryCodes = recoveryCodes?.ToArray() ?? Array.Empty<string>(),
            },
            Meta));
    }

    /// <summary>
    /// Desactiva el 2FA tras verificar un código válido (no basta con estar
    /// autenticado: exige demostrar posesión del segundo factor).
    /// </summary>
    [HttpPost("disable")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Disable([FromBody] MfaCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
        {
            return BadRequest(ApiResponse.Fail(
                ErrorCodes.GenValidationFailed, "El código es obligatorio.", null, Meta));
        }

        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized(ApiResponse.Fail(
                ErrorCodes.AuthInvalidCredentials, "Sesión no válida.", null, Meta));
        }

        if (!user.TwoFactorEnabled)
        {
            return BadRequest(ApiResponse.Fail(
                ErrorCodes.GenValidationFailed, "El doble factor no está activado.", null, Meta));
        }

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            request.Code.Replace(" ", string.Empty));

        if (!isValid)
        {
            return BadRequest(ApiResponse.Fail(
                ErrorCodes.AuthInvalidCredentials, "El código no es válido.", null, Meta));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        // Invalida el secreto: reactivar exigirá un enable nuevo
        await _userManager.ResetAuthenticatorKeyAsync(user);

        _logger.LogInformation("2FA desactivado para el usuario {UserId}", user.Id);

        return Ok(ApiResponse.Ok<object>(
            new { twoFactorEnabled = false }, Meta));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Task<User?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue("sub");
        return userId is null ? Task.FromResult<User?>(null) : _userManager.FindByIdAsync(userId);
    }

    private static string BuildOtpauthUri(string email, string unformattedKey)
    {
        // Formato estándar otpauth://totp/{issuer}:{cuenta}?secret=...&issuer=...
        // que interpretan Google Authenticator, Authy, 1Password, etc.
        return string.Format(
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            UrlEncoder.Default.Encode(Issuer),
            UrlEncoder.Default.Encode(email),
            unformattedKey);
    }

    private static string FormatKey(string unformattedKey)
    {
        // Agrupa en bloques de 4 para lectura/tecleo manual
        var result = new StringBuilder();
        for (var i = 0; i < unformattedKey.Length; i += 4)
        {
            var chunk = Math.Min(4, unformattedKey.Length - i);
            result.Append(unformattedKey.AsSpan(i, chunk)).Append(' ');
        }
        return result.ToString().Trim().ToLowerInvariant();
    }
}

/// <summary>Cuerpo de confirm/disable: el código TOTP de 6 dígitos.</summary>
public class MfaCodeRequest
{
    public string Code { get; set; } = string.Empty;
}