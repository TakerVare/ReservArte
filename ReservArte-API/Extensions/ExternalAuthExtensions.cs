using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace ReservArte.API.Extensions;

public static class ExternalAuthExtensions
{
    /// <summary>
    /// Registra los esquemas de login social (vol. 1 §4.4.1): una cookie
    /// efímera (IdentityConstants.ExternalScheme) donde los handlers OAuth
    /// depositan el principal externo hasta que el callback lo procesa, y
    /// los proveedores Google/Apple/Instagram SOLO si su configuración
    /// existe — con credenciales vacías el handler revienta el arranque,
    /// y así la API arranca en cualquier máquina.
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
                // en el redirect GET de vuelta (None exigiría Secure+HTTPS), y
                // SameAsRequest emite la cookie sin flag Secure en HTTP (en
                // prod, con HTTPS, vuelve a llevarlo automáticamente). Safari
                // rechaza cookies Secure sobre HTTP incluso en localhost.
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                options.Events.OnRemoteFailure = HandleRemoteFailure;
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

                options.Events.OnRemoteFailure = HandleRemoteFailure;
            });
        }

        var metaAppId = configuration["Authentication:Meta:AppId"];
        if (!string.IsNullOrWhiteSpace(metaAppId))
        {
            // Instagram no expone un "Sign in" independiente: se implementa
            // vía plataforma Meta (Facebook Login) con esquema dedicado
            // "Instagram" (convención del vol. 2 §9.2 / vol. 1 §4.4.1); ese
            // nombre de esquema acaba como LoginProvider en AspNetUserLogins.
            // Requisitos de consola Meta: dominios de la app (localhost),
            // plataforma web y permiso "email" añadido al caso de uso.
            authBuilder.AddFacebook("Instagram", options =>
            {
                options.AppId = metaAppId;
                options.AppSecret =
                    configuration["Authentication:Meta:AppSecret"] ?? string.Empty;
                options.SignInScheme = IdentityConstants.ExternalScheme;

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                options.Events.OnRemoteFailure = HandleRemoteFailure;
            });
        }

        return services;
    }

    /// <summary>
    /// Fallos del intercambio remoto (usuario cancela el consentimiento,
    /// scopes rechazados, state caducado...): aterrizaje digno en la SPA
    /// con #error en lugar de página de excepción. El error concreto queda
    /// en logs del servidor; al cliente no se le filtra el motivo.
    /// </summary>
    private static Task HandleRemoteFailure(RemoteFailureContext context)
    {
        var origin = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()?
            .FirstOrDefault() ?? "http://localhost:3000";

        context.Response.Redirect($"{origin}/auth/callback#error=external_auth_failed");
        context.HandleResponse();

        return Task.CompletedTask;
    }
}