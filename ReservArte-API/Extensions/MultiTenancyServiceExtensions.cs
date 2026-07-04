using ReservArte.API.Options;
using ReservArte.API.Services;
using ReservArte.Domain.Interfaces;

namespace ReservArte.API.Extensions;

public static class MultiTenancyServiceExtensions
{
    /// <summary>
    /// Registra el binding de la sección MultiTenant y el holder scoped
    /// del tenant actual que rellena TenantMiddleware en cada petición.
    /// </summary>
    public static IServiceCollection AddMultiTenancy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MultiTenantOptions>(
            configuration.GetSection(MultiTenantOptions.SectionName));

        services.AddScoped<ICurrentOrganizationService, CurrentOrganizationService>();

        return services;
    }
}