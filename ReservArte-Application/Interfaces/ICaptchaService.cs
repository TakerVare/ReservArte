namespace ReservArte.Application.Interfaces;

/// <summary>
/// Verifica el token de CAPTCHA que el frontend adjunta al login tras varios
/// intentos fallidos (vol. 1 §4.4.3). En dev (Captcha:Enabled = false) la
/// verificación se omite y siempre autoriza.
/// </summary>
public interface ICaptchaService
{
    Task<bool> VerifyAsync(string? token, string? remoteIp);
}