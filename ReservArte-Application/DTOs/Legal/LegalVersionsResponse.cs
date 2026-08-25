namespace ReservArte.Application.DTOs.Legal;

/// <summary>
/// Versiones vigentes de los documentos legales (términos y política de
/// privacidad). Respuesta de GET /api/v1/legal/versions.
/// v1: versiones globales del despliegue (LegalDocumentsOptions).
/// Fase 3 (multi-tenant): evolucionará a versiones por organización.
/// </summary>
public class LegalVersionsResponse
{
    public string TermsVersion { get; set; } = string.Empty;
    public string PrivacyVersion { get; set; } = string.Empty;
}