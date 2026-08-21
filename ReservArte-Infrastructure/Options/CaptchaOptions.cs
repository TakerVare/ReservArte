namespace ReservArte.Infrastructure.Options;

/// <summary>
/// Sección "Captcha" de configuración. En dev, Enabled = false omite la
/// verificación (la infraestructura externa no bloquea el desarrollo local);
/// en producción se activa y la SecretKey vive en User Secrets / Secrets
/// Manager. Por defecto se valida contra Cloudflare Turnstile; la URL es
/// configurable para usar reCAPTCHA u otro proveedor compatible.
/// </summary>
public class CaptchaOptions
{
    public const string SectionName = "Captcha";

    /// <summary>Si es false, la verificación se omite (dev). Por defecto false.</summary>
    public bool Enabled { get; set; }

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Endpoint de verificación del proveedor (Turnstile por defecto).</summary>
    public string VerifyUrl { get; set; } =
        "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}