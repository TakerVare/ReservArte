namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Canje del ticket intermedio de 2FA por el JWT final (POST /auth/mfa/verify,
/// vol. 1 §4.4.2). El campo Code admite un código TOTP de 6 dígitos O un
/// código de recuperación (RA-869d7ezgy, Fase 3).
/// </summary>
public class MfaVerifyRequest
{
    public string MfaTicket { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}