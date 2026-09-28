using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Validators.Auth;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Services;
using ReservArte.Shared.Api;


namespace ReservArte.API.Extensions;

public static class AuthServiceExtensions
{
    /// <summary>Valor de Email:Provider que escribe los correos en archivo (desarrollo).</summary>
    public const string EmailProviderFile = "File";

    /// <summary>
    /// Registra el binding de la sección "Jwt" (vol. 1 §5.1.3), el emisor
    /// de tokens, el servicio de flujos de autenticación, los validadores
    /// FluentValidation y el esquema de autenticación JwtBearer que protege
    /// los endpoints [Authorize]. Los parámetros de validación replican los
    /// de JwtTokenService.ValidateToken (misma clave, issuer, audience y
    /// ClockSkew cero), y RoleClaimType = "role" para que [Authorize(Roles)]
    /// lea el claim corto que emitimos.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.Configure<CaptchaOptions>(
            configuration.GetSection(CaptchaOptions.SectionName));
        // Fail-fast: sin versiones de documentos legales configuradas (vacías
        // en el appsettings base; se rellenan por entorno — Development o
        // variables de entorno en producción), la API no arranca. Evita un
        // fallo silencioso en el registro RGPD por config olvidada al desplegar.
        services.AddOptions<LegalDocumentsOptions>()
            .Bind(configuration.GetSection(LegalDocumentsOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.TermsVersion)
                    && !string.IsNullOrWhiteSpace(o.PrivacyVersion),
                "LegalDocuments:TermsVersion y LegalDocuments:PrivacyVersion deben estar configurados en este entorno (vacíos en appsettings base; configúralos en Development o por variables de entorno en producción).")
            .ValidateOnStart();

        // App: URL base del frontend (para enlaces como el reset de contraseña).
        // Fail-fast: sin ella no se pueden construir enlaces válidos.
        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.FrontendBaseUrl),
                "App:FrontendBaseUrl debe estar configurado en este entorno (vacío en appsettings base; configúralo en Development o por variables de entorno en producción).")
            .ValidateOnStart();

        // Email: el proveedor lo elige la configuración (Email:Provider), no el
        // entorno. «File» escribe los correos en ./sent-emails/ (desarrollo).
        // SES llegará con RA-869d7f65a como otro valor de esta misma clave.
        // Fail-fast: sin proveedor válido, AuthService quedaría irresoluble y
        // tumbaría la autenticación en la primera petición; mejor que la API no
        // arranque, con un mensaje claro.
        var emailProvider = configuration["Email:Provider"];
        if (string.Equals(emailProvider, EmailProviderFile, StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailService, DevFileEmailService>();
        }
        else
        {
            throw new InvalidOperationException(
                $"Email:Provider '{emailProvider}' no es un proveedor soportado. " +
                $"Valores válidos: {EmailProviderFile} (escribe en ./sent-emails/). " +
                "Configúralo por entorno (Development o variables de entorno).");
        }

        services.AddHttpClient<ICaptchaService, CaptchaService>();

        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var secretKey = jwtSection["SecretKey"] ?? string.Empty;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Sin remapeo de claims de Microsoft: las claves entrantes se
                // leen tal cual las emite JwtTokenService (sub, email, role,
                // organization_id), no se traducen a las URIs largas de
                // ClaimTypes. Imprescindible para leer "sub" por su nombre.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    // [Authorize(Roles=...)] leerá el claim corto "role".
                    // NameClaimType se deja por defecto: no designamos "sub"
                    // como claim de nombre para poder leerlo por su clave.
                    RoleClaimType = "role",
                };

                options.Events = new JwtBearerEvents
                {
                    // Un ticket intermedio de 2FA (claim mfa_pending) es un
                    // JWT válido en firma, pero NO autoriza operaciones: se
                    // rechaza en cualquier endpoint [Authorize]. Solo vale
                    // para el canje en /auth/mfa/verify (que lo lee aparte).
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.FindFirst("mfa_pending") is not null)
                        {
                            context.Fail("El ticket de 2FA no autoriza esta operación.");
                        }

                        return Task.CompletedTask;
                    },

                    // Sin estos dos eventos, el 401 del challenge y el 403 de
                    // [Authorize(Roles)] salen SIN cuerpo: los emite el
                    // middleware, sin pasar por controladores ni envelope, y el
                    // cliente se queda sin error.code (RA-869f1anz3).
                    OnChallenge = async context =>
                    {
                        // Suprime la respuesta por defecto; por eso la cabecera
                        // WWW-Authenticate (RFC 6750) hay que reponerla a mano.
                        context.HandleResponse();

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.Headers.WWWAuthenticate = BuildWwwAuthenticate(context);

                        await WriteEnvelopeAsync(
                            context.HttpContext,
                            ErrorCodes.GenUnauthorized,
                            "Se requiere una sesión válida para acceder a este recurso.");
                    },

                    // El handler ya fija el 403 antes de invocar el evento:
                    // solo falta el cuerpo. GEN_FORBIDDEN significa «no tienes
                    // permiso», NO «tu sesión no vale»: la SPA no debe cerrar
                    // sesión por él (no entra en SESSION_ENDING_ERROR_CODES).
                    OnForbidden = context =>
                        WriteEnvelopeAsync(
                            context.HttpContext,
                            ErrorCodes.GenForbidden,
                            "No tienes permiso para realizar esta operación."),
                };
            });

        return services;
    }

    private static Task WriteEnvelopeAsync(HttpContext httpContext, string code, string message) =>
        httpContext.Response.WriteAsJsonAsync(ApiResponse.Fail(
            code,
            message,
            details: null,
            meta: ApiMeta.Create(httpContext.TraceIdentifier)));

    /// <summary>
    /// Replica la cabecera que el handler JwtBearer emitiría por su cuenta:
    /// `Bearer` a secas si no había token, y con `error="invalid_token"` y su
    /// descripción si el token llegó pero no superó la validación.
    /// </summary>
    private static string BuildWwwAuthenticate(JwtBearerChallengeContext context)
    {
        var header = context.Options.Challenge;

        if (string.IsNullOrEmpty(context.Error))
        {
            return header;
        }

        header += $" error=\"{context.Error}\"";

        if (!string.IsNullOrEmpty(context.ErrorDescription))
        {
            header += $", error_description=\"{context.ErrorDescription}\"";
        }

        return header;
    }
}
