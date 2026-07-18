using Microsoft.AspNetCore.Identity;

namespace ReservArte.API.Extensions;

public static class ExternalAuthExtensions
{
    /// <summary>
    /// Registra los esquemas de login social (vol. 1 §4.4.1): una cookie
    /// efímera (IdentityConstants.ExternalScheme) donde los handlers OAuth
    /// depositan el principal externo hasta que el callback lo procesa, y
    /// los proveedores Google/Apple SOLO si su configuración existe — con
    /// credenciales vacías el handler revienta el arranque, y así la API
    /// arranca en cualquier máquina. Meta/Instagram llega en RA-869d7ezbm.
    /// </summary>
    public static IServiceCollection AddExternalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authBuilder = services.AddAuthentication();

        authBuilder.AddCookie(IdentityConstants.ExternalScheme);

        var googleClientId = configuration["Authentication:Google:ClientId"];
        if (!string.IsNullOrWhiteSpace(googleClientId))
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret =
                    configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
                options.SignInScheme = IdentityConstants.ExternalScheme;

                // Dev corre en HTTP: Lax permite enviar la cookie de correlación
                // en el redirect GET de vuelta desde Google (el default None
                // exigiría Secure+HTTPS). El flujo de Google es siempre redirect
                // GET, así que Lax también es válido en producción.
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            });
        }

        var appleClientId = configuration["Authentication:Apple:ClientId"];
        if (!string.IsNullOrWhiteSpace(appleClientId))
        {
            authBuilder.AddApple(options =>
            {
                options.ClientId = appleClientId;
                options.TeamId = configuration["Authentication:Apple:TeamId"] ?? string.Empty;
                options.KeyId = configuration["Authentication:Apple:KeyId"] ?? string.Empty;
                options.SignInScheme = IdentityConstants.ExternalScheme;

                // Apple usa response_mode=form_post: exige HTTPS y SameSite=None
                // (defaults del handler). Verificable cuando existan credenciales
                // de Apple Developer y la API corra con certificado.
                var privateKey = configuration["Authentication:Apple:PrivateKey"];
                if (!string.IsNullOrWhiteSpace(privateKey))
                {
                    options.GenerateClientSecret = true;
                    options.PrivateKey = (_, _) => Task.FromResult(privateKey.AsMemory());
                }
            });
        }

        return services;
    }
}