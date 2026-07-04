namespace ReservArte.API.Options;

/// <summary>
/// Sección MultiTenant del contrato de configuración (vol. 1 §5.1.3).
/// Dev: ResolutionStrategy = "Header" + HeaderName. Prod: "Subdomain" + BaseDomain.
/// </summary>
public class MultiTenantOptions
{
    public const string SectionName = "MultiTenant";

    public string ResolutionStrategy { get; set; } = string.Empty;

    public string HeaderName { get; set; } = string.Empty;

    public string BaseDomain { get; set; } = string.Empty;

    public string DefaultOrganizationId { get; set; } = string.Empty;
}