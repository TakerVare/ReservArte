namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Petición de registro local (POST /api/v1/auth/register).
/// La organización se resuelve por el TenantMiddleware (cabecera en dev,
/// subdominio en prod), no viaja en el cuerpo.
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    // Consentimiento RGPD (vol. 1 §4.4.1). El cliente envía la versión de los
    // documentos que se le mostraron y aceptó; el backend valida que coinciden
    // con las versiones vigentes y persiste la aceptación con su timestamp.
    public bool AcceptedTerms { get; set; }
    public bool AcceptedPrivacy { get; set; }
    public string? AcceptedTermsVersion { get; set; }
    public string? AcceptedPrivacyVersion { get; set; }
}