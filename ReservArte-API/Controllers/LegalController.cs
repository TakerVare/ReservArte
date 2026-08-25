using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ReservArte.Application.DTOs.Legal;
using ReservArte.Infrastructure.Options;
using ReservArte.Shared.Api;

namespace ReservArte.API.Controllers;

[ApiController]
[Route("api/v1/legal")]
public class LegalController : ControllerBase
{
    private readonly LegalDocumentsOptions _legalDocuments;

    public LegalController(IOptions<LegalDocumentsOptions> legalDocuments)
    {
        _legalDocuments = legalDocuments.Value;
    }

    private ApiMeta Meta => ApiMeta.Create(HttpContext.TraceIdentifier);

    /// <summary>
    /// Versiones vigentes de los documentos legales (términos y privacidad).
    /// Público: se consume en el registro, antes de que exista sesión. El
    /// frontend las envía en el consentimiento; el backend valida coincidencia.
    /// v1: versiones globales de configuración. Fase 3: por organización.
    /// </summary>
    [HttpGet("versions")]
    [ProducesResponseType(typeof(ApiResponse<LegalVersionsResponse>), StatusCodes.Status200OK)]
    public IActionResult GetVersions()
    {
        var data = new LegalVersionsResponse
        {
            TermsVersion = _legalDocuments.TermsVersion,
            PrivacyVersion = _legalDocuments.PrivacyVersion,
        };
        return Ok(ApiResponse.Ok(data, Meta));
    }
}