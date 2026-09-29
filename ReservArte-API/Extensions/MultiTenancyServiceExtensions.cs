using ReservArte.API.Options;
using ReservArte.API.Services;
using ReservArte.Domain.Interfaces;

namespace ReservArte.API.Extensions;

public static class MultiTenancyServiceExtensions
{
    /// <summary>
    /// Registra el binding de la sección MultiTenant y el holder scoped
    /// del tenant actual que rellena TenantMiddleware en cada petición.
    /// Fail-fast, como App y LegalDocuments: una sección mal configurada
    /// impide arrancar la API en vez de convertir cada petición en un 400.
    /// Fuera de Development, la cabecera y la organización por defecto están
    /// prohibidas: permitirían elegir el tenant desde el cliente o caer en uno
    /// sin pedirlo (en producción el tenant sale del subdominio).
    /// </summary>
    public static IServiceCollection AddMultiTenancy(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var isDevelopment = environment.IsDevelopment();

        services.AddOptions<MultiTenantOptions>()
            .Bind(configuration.GetSection(MultiTenantOptions.SectionName))
            .Validate(
                o => IsStrategy(o, MultiTenantOptions.StrategyHeader)
                    || IsStrategy(o, MultiTenantOptions.StrategySubdomain),
                $"MultiTenant:ResolutionStrategy debe ser {MultiTenantOptions.StrategyHeader} o " +
                $"{MultiTenantOptions.StrategySubdomain} (vacío en appsettings base; configúralo por entorno).")
            .Validate(
                o => !IsStrategy(o, MultiTenantOptions.StrategySubdomain)
                    || !string.IsNullOrWhiteSpace(o.BaseDomain),
                "MultiTenant:BaseDomain es obligatorio con la estrategia Subdomain.")
            .Validate(
                o => string.IsNullOrWhiteSpace(o.DefaultOrganizationId)
                    || Guid.TryParse(o.DefaultOrganizationId, out _),
                "MultiTenant:DefaultOrganizationId debe estar vacío o ser un GUID.")
            .Validate(
                o => isDevelopment || !IsStrategy(o, MultiTenantOptions.StrategyHeader),
                "MultiTenant:ResolutionStrategy Header solo se permite en Development; fuera de él, Subdomain.")
            .Validate(
                o => isDevelopment || string.IsNullOrWhiteSpace(o.DefaultOrganizationId),
                "MultiTenant:DefaultOrganizationId solo se permite en Development.")
            .ValidateOnStart();

        services.AddScoped<ICurrentOrganizationService, CurrentOrganizationService>();

        return services;
    }

    private static bool IsStrategy(MultiTenantOptions options, string strategy) =>
        string.Equals(options.ResolutionStrategy, strategy, StringComparison.OrdinalIgnoreCase);
}
