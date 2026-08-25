namespace ReservArte.Infrastructure.Options;

/// <summary>
/// Versiones vigentes de los documentos legales (términos y política de
/// privacidad). Enlazadas desde la sección "LegalDocuments" de la
/// configuración. Se usan para validar y persistir el consentimiento RGPD
/// en el registro (vol. 1 §4.4.1).
/// </summary>
public class LegalDocumentsOptions
{
    public const string SectionName = "LegalDocuments";

    public string TermsVersion { get; set; } = string.Empty;
    public string PrivacyVersion { get; set; } = string.Empty;
}