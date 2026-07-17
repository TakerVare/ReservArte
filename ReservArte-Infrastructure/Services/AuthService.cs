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

namespace ReservArte.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly AppDbContext _context;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        AppDbContext context,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<AuthResult<AuthResponse>> LoginAsync(
        LoginRequest request, Guid organizationId, string? ipAddress)
    {
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

        // TODO(RA-869d7ezgy): si user.TwoFactorEnabled, devolver aquí el
        // estado intermedio mfa_required (ticket de un solo uso) en lugar
        // de emitir tokens. Hoy ningún usuario tiene 2FA activa.

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