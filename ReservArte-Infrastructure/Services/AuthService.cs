using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReservArte.Application.DTOs.Auth;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Shared.Api;
using Microsoft.IdentityModel.JsonWebTokens;


namespace ReservArte.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly AppDbContext _context;
    private readonly JwtOptions _jwtOptions;
    private readonly ICaptchaService _captchaService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        AppDbContext context,
        IOptions<JwtOptions> jwtOptions,
        ICaptchaService captchaService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _jwtOptions = jwtOptions.Value;
        _captchaService = captchaService;
        _logger = logger;
    }

    public async Task<AuthResult<AuthResponse>> LoginAsync(
        LoginRequest request, Guid organizationId, string? ipAddress)
    {
        // CAPTCHA: el frontend lo adjunta tras varios intentos fallidos. En
        // dev (Captcha:Enabled = false) VerifyAsync siempre autoriza; con él
        // activo, un token ausente o inválido detiene el login.
        if (!await _captchaService.VerifyAsync(request.Captcha, ipAddress))
        {
            return AuthResult<AuthResponse>.Fail(
                ErrorCodes.GenValidationFailed,
                "La verificación de seguridad (CAPTCHA) no es válida.");
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        // Respuesta opaca idéntica para: usuario inexistente, usuario de
        // otra organización, cuenta solo social (sin contraseña local) o
        // contraseña incorrecta. No filtrar cuál fue el motivo.
        if (user is null ||
            user.OrganizationId != organizationId ||
            !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return AuthResult<AuthResponse>.Fail(
                ErrorCodes.AuthInvalidCredentials,
                "Email o contraseña incorrectos.");
        }

        // 2FA activo: no se emiten tokens todavía. Se devuelve un ticket
        // intermedio que el cliente canjea en /auth/mfa/verify con el código.
        if (user.TwoFactorEnabled)
        {
            _logger.LogInformation(
                "Login del usuario {UserId} pendiente de segundo factor", user.Id);

            return AuthResult<AuthResponse>.Ok(new AuthResponse
            {
                MfaRequired = true,
                MfaTicket = _jwtTokenService.GenerateMfaTicket(user, organizationId),
            });
        }

        var response = await IssueTokensAsync(user, ipAddress);

        _logger.LogInformation("Login correcto del usuario {UserId}", user.Id);

        return AuthResult<AuthResponse>.Ok(response);
    }

    public async Task<AuthResult<AuthResponse>> RegisterAsync(
        RegisterRequest request, Guid organizationId, string? ipAddress)
    {
        var user = new User
        {
            OrganizationId = organizationId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.Phone,
            // Mínimo privilegio por defecto. El alta de administradores y el
            // onboarding SaaS de organizaciones (Fase 3 del producto)
            // definirán la asignación de roles definitiva.
            Rol = "employee",
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return AuthResult<AuthResponse>.Fail(
                    ErrorCodes.GenConflict,
                    "Ya existe una cuenta con ese email.");
            }

            // Resto de errores de Identity (política de contraseña, formato):
            // detalle por campo con la convención { field, code, message }
            var details = result.Errors
                .Select(e => new ApiErrorDetail
                {
                    Field = e.Code.Contains("Password") ? "password" : "email",
                    Code = e.Code,
                    Message = e.Description,
                })
                .ToList();

            return AuthResult<AuthResponse>.Fail(
                ErrorCodes.GenValidationFailed,
                "El registro no supera las validaciones.",
                details);
        }

        var response = await IssueTokensAsync(user, ipAddress);

        _logger.LogInformation("Registro correcto del usuario {UserId}", user.Id);

        return AuthResult<AuthResponse>.Ok(response);
    }

    public async Task<AuthResult<AuthResponse>> RefreshTokenAsync(
        string refreshToken, string? ipAddress)
    {
        var stored = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
        {
            return AuthResult<AuthResponse>.Fail(
                ErrorCodes.AuthRefreshInvalid,
                "El refresh token no es válido o ha expirado.");
        }

        // Rotación (vol. 2 §9.2.2): el token usado se revoca y se emite un
        // par nuevo. El SaveChanges de IssueTokensAsync persiste ambas cosas.
        stored.IsRevoked = true;

        var response = await IssueTokensAsync(stored.User, ipAddress);

        return AuthResult<AuthResponse>.Ok(response);
    }

        public async Task<AuthResult<AuthResponse>> VerifyMfaAsync(
        string mfaTicket, string code, Guid organizationId, string? ipAddress)
    {
        // 1) El ticket debe ser un JWT válido (firma/emisor/audiencia/vigencia)
        //    y llevar la marca mfa_pending. ValidateToken cubre lo primero.
        var principal = _jwtTokenService.ValidateToken(mfaTicket);

        if (principal is null ||
            principal.FindFirst(JwtTokenService.MfaPendingClaimType) is null)
        {
            return AuthResult<AuthResponse>.Fail(
                ErrorCodes.AuthInvalidCredentials,
                "El ticket de verificación no es válido o ha caducado.");
        }

        // 2) El sujeto del ticket debe existir, tener 2FA activo y pertenecer
        //    a la organización de la petición (coherencia multi-tenant)
        var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var user = userId is null ? null : await _userManager.FindByIdAsync(userId);

        if (user is null ||
            !user.TwoFactorEnabled ||
            user.OrganizationId != organizationId)
        {
            return AuthResult<AuthResponse>.Fail(
                ErrorCodes.AuthInvalidCredentials,
                "El ticket de verificación no es válido.");
        }

        // 3) El código: primero TOTP; si no cuela, se intenta como código de
        //    recuperación (un solo uso). El de recuperación se implementa por
        //    completo en la Fase 3; aquí ya queda cableado el canje.
        var sanitized = code.Replace(" ", string.Empty);

        var totpValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, sanitized);

        var accepted = totpValid;

        if (!totpValid)
        {
            var redeem = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, sanitized);
            accepted = redeem.Succeeded;
        }

        if (!accepted)
        {
            return AuthResult<AuthResponse>.Fail(
                ErrorCodes.AuthInvalidCredentials,
                "El código no es válido. Revisa la hora del dispositivo o usa un código de recuperación.");
        }

        // 4) Segundo factor superado: se emite el par definitivo
        var response = await IssueTokensAsync(user, ipAddress);

        _logger.LogInformation(
            "Verificación de segundo factor correcta para el usuario {UserId}", user.Id);

        return AuthResult<AuthResponse>.Ok(response);
    }

    public async Task<AuthResult<AuthResponse>> ExternalLoginAsync(
        string provider,
        string providerKey,
        string? email,
        string? firstName,
        string? lastName,
        Guid organizationId,
        string? ipAddress)
    {
        // 1) ¿Existe ya el vínculo proveedor+sujeto en AspNetUserLogins?
        var user = await _userManager.FindByLoginAsync(provider, providerKey);

        if (user is not null)
        {
            if (user.OrganizationId != organizationId)
            {
                // Cuenta de otra organización: respuesta opaca, sin detalles
                return AuthResult<AuthResponse>.Fail(
                    ErrorCodes.AuthInvalidCredentials,
                    "No se pudo completar el inicio de sesión.");
            }

            // TODO(RA-869d7ezgy): si user.TwoFactorEnabled, estado intermedio
            // mfa_required antes de emitir tokens (mismo criterio que el login local)

            var existingResponse = await IssueTokensAsync(user, ipAddress);

            _logger.LogInformation(
                "Login social correcto ({Provider}) del usuario {UserId}", provider, user.Id);

            return AuthResult<AuthResponse>.Ok(existingResponse);
        }

        // Sin vínculo previo: el email del IdP es imprescindible para
        // vincular o crear cuenta
        if (string.IsNullOrWhiteSpace(email))
        {
            return AuthResult<AuthResponse>.Fail(
                ErrorCodes.GenValidationFailed,
                "El proveedor no ha facilitado un email verificado.");
        }

        // 2) ¿Existe un usuario con ese email? → vincular proveedor (§4.4.1:
        //    mismo email = enlazar a la cuenta existente)
        user = await _userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            if (user.OrganizationId != organizationId)
            {
                return AuthResult<AuthResponse>.Fail(
                    ErrorCodes.AuthInvalidCredentials,
                    "No se pudo completar el inicio de sesión.");
            }

            var linkResult = await _userManager.AddLoginAsync(
                user, new UserLoginInfo(provider, providerKey, provider));

            if (!linkResult.Succeeded)
            {
                var linkErrors = string.Join("; ", linkResult.Errors.Select(e => e.Description));
                _logger.LogWarning(
                    "No se pudo vincular {Provider} al usuario {UserId}: {Errors}",
                    provider, user.Id, linkErrors);

                return AuthResult<AuthResponse>.Fail(
                    ErrorCodes.AuthInvalidCredentials,
                    "No se pudo completar el inicio de sesión.");
            }

            _logger.LogInformation(
                "Proveedor {Provider} vinculado al usuario existente {UserId}", provider, user.Id);
        }
        else
        {
            // 3) Alta de cuenta solo-social: sin contraseña local
            //    (PasswordHash NULL, previsto en el esquema — vol. 1 §5)
            user = new User
            {
                OrganizationId = organizationId,
                FirstName = firstName ?? string.Empty,
                LastName = lastName ?? string.Empty,
                UserName = email,
                Email = email,
                // El email llega verificado por el IdP (Google solo emite
                // emails verificados; Apple entrega email real o relay propio)
                EmailConfirmed = true,
                Rol = "employee",
            };

            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                var createErrors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                _logger.LogWarning(
                    "No se pudo crear la cuenta solo-social ({Provider}): {Errors}",
                    provider, createErrors);

                return AuthResult<AuthResponse>.Fail(
                    ErrorCodes.AuthInvalidCredentials,
                    "No se pudo completar el inicio de sesión.");
            }

            await _userManager.AddLoginAsync(
                user, new UserLoginInfo(provider, providerKey, provider));

            _logger.LogInformation(
                "Cuenta solo-social creada ({Provider}) para el usuario {UserId}", provider, user.Id);
        }

        var response = await IssueTokensAsync(user, ipAddress);

        return AuthResult<AuthResponse>.Ok(response);
    }

    public async Task ForgotPasswordAsync(string email, Guid organizationId)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || user.OrganizationId != organizationId)
        {
            // Anti-enumeración: el llamador responde igual exista o no
            return;
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        // TODO(SES, tareas de Infrastructure): enviar email con el enlace
        // /reset-password/{token}. Hasta que exista el proveedor de email,
        // el token no sale del servidor. NUNCA registrar el token en logs.
        _logger.LogInformation(
            "Solicitud de recuperación de contraseña procesada para el usuario {UserId}",
            user.Id);

        _ = resetToken;
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, string? ipAddress)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user, user.OrganizationId);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            IsRevoked = false,
            CreatedByIp = ipAddress,
        });

        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Rol = user.Rol,
            },
        };
    }
}