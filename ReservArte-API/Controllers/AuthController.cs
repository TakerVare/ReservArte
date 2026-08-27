using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ReservArte.Application.DTOs.Auth;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;
using Microsoft.AspNetCore.RateLimiting;
using ReservArte.API.Extensions;

namespace ReservArte.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotValidator;
    private readonly IValidator<ResetPasswordRequest> _resetValidator;
    private readonly IValidator<MfaVerifyRequest> _mfaVerifyValidator;

    public AuthController(
        IAuthService authService,
        ICurrentOrganizationService currentOrganization,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator,
        IValidator<RefreshTokenRequest> refreshValidator,
        IValidator<ForgotPasswordRequest> forgotValidator,
        IValidator<ResetPasswordRequest> resetValidator,
        IValidator<MfaVerifyRequest> mfaVerifyValidator)
    {
        _mfaVerifyValidator = mfaVerifyValidator;
        _authService = authService;
        _currentOrganization = currentOrganization;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _refreshValidator = refreshValidator;
        _forgotValidator = forgotValidator;
        _resetValidator = resetValidator;
    }

    /// <summary>Login local con email y contraseña (vol. 1 §4.4.1).</summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingServiceExtensions.LoginPolicy)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var invalid = await ValidateAsync(_loginValidator, request);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _authService.LoginAsync(request, OrganizationId, ClientIp);

        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!, Meta))
            : FromAuthFailure(result);
    }

    /// <summary>Registro local dentro de la organización resuelta.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var invalid = await ValidateAsync(_registerValidator, request);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _authService.RegisterAsync(request, OrganizationId, ClientIp);

        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!, Meta))
            : FromAuthFailure(result);
    }

    /// <summary>Renovación de tokens con rotación (el refresh usado queda revocado).</summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        var invalid = await ValidateAsync(_refreshValidator, request);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ClientIp);

        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!, Meta))
            : FromAuthFailure(result);
    }

    /// <summary>
    /// Canjea el ticket intermedio de 2FA + el código (TOTP o de recuperación)
    /// por el par de tokens definitivo. Anónimo: el usuario aún no tiene sesión.
    /// </summary>
    [HttpPost("mfa/verify")]
    [EnableRateLimiting(RateLimitingServiceExtensions.MfaVerifyPolicy)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyMfa(MfaVerifyRequest request)
    {
        var invalid = await ValidateAsync(_mfaVerifyValidator, request);
        if (invalid is not null)
        {
            return invalid;
        }

        var result = await _authService.VerifyMfaAsync(
            request.MfaTicket, request.Code, OrganizationId, ClientIp);

        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!, Meta))
            : FromAuthFailure(result);
    }

    /// <summary>
    /// Solicita la recuperación de contraseña. Responde SIEMPRE igual,
    /// exista o no el email (anti-enumeración). El envío del correo se
    /// conectará cuando exista el proveedor SES (tareas de Infrastructure).
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var invalid = await ValidateAsync(_forgotValidator, request);
        if (invalid is not null)
        {
            return invalid;
        }

        await _authService.ForgotPasswordAsync(request.Email, OrganizationId);
        return Ok(ApiResponse.Ok<object>(
            new
            {
                message = "Si el email existe, recibirás instrucciones para restablecer la contraseña.",
            },
            Meta));
    }

    /// <summary>
    /// Restablece la contraseña con el token recibido por email (vol. 1 §4.4.1).
    /// Respuesta opaca ante token/email inválidos (anti-enumeración).
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var invalid = await ValidateAsync(_resetValidator, request);
        if (invalid is not null)
        {
            return invalid;
        }
        var result = await _authService.ResetPasswordAsync(request, OrganizationId);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!, Meta))
            : FromAuthFailure(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Garantizada por TenantMiddleware: ninguna petición llega a /api/*
    /// sin organización resuelta.
    /// </summary>
    private Guid OrganizationId => _currentOrganization.OrganizationId!.Value;

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    private ApiMeta Meta => ApiMeta.Create(HttpContext.TraceIdentifier);

    private async Task<IActionResult?> ValidateAsync<T>(IValidator<T> validator, T request)
    {
        var validation = await validator.ValidateAsync(request);

        if (validation.IsValid)
        {
            return null;
        }

        var details = validation.Errors
            .Select(e => new ApiErrorDetail
            {
                Field = ToCamelCase(e.PropertyName),
                Code = e.ErrorCode,
                Message = e.ErrorMessage,
            })
            .ToList();

        return BadRequest(ApiResponse.Fail(
            ErrorCodes.GenValidationFailed,
            "La petición no supera las validaciones.",
            details,
            Meta));
    }

    private IActionResult FromAuthFailure<T>(AuthResult<T> result)
    {
        var statusCode = result.ErrorCode switch
        {
            ErrorCodes.AuthInvalidCredentials => StatusCodes.Status401Unauthorized,
            ErrorCodes.AuthRefreshInvalid => StatusCodes.Status401Unauthorized,
            ErrorCodes.GenConflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return StatusCode(statusCode, ApiResponse.Fail(
            result.ErrorCode!,
            result.ErrorMessage!,
            result.ErrorDetails,
            Meta));
    }

    private static string ToCamelCase(string propertyName) =>
        string.IsNullOrEmpty(propertyName)
            ? propertyName
            : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}