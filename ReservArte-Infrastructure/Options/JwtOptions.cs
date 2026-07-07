namespace ReservArte.Infrastructure.Options;

/// <summary>
/// Sección "Jwt" del contrato de configuración (vol. 1 §5.1.3).
/// El SecretKey NUNCA está en el repositorio: en dev vive en User Secrets,
/// en producción en variables de entorno / AWS Secrets Manager.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; }

    public int RefreshTokenDays { get; set; }
}