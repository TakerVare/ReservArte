namespace ReservArte.Infrastructure.Options;

/// <summary>
/// Configuración general de la aplicación. FrontendBaseUrl: URL base del SPA,
/// usada para construir enlaces que apuntan al frontend (p. ej. el enlace de
/// restablecimiento de contraseña). v1: URL única. Fase 3 (multi-tenant): el
/// enlace será por subdominio de organización.
/// </summary>
public class AppOptions
{
    public const string SectionName = "App";

    public string FrontendBaseUrl { get; set; } = string.Empty;
}