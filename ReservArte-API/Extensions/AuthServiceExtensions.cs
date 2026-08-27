using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Validators.Auth;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Services;
using Microsoft.Extensions.Hosting;


namespace ReservArte.API.Extensions;

public static class AuthServiceExtensions
{
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
        IConfiguration configuration,
        IHostEnvironment environment)
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

        // Email: en desarrollo escribe a archivo ({contentRoot}/sent-emails/);
        // en producción usará SES (tarea de infraestructura futura).
        if (environment.IsDevelopment())
        {
            services.AddScoped<IEmailService, DevFileEmailService>();
        }
        // else: services.AddScoped<IEmailService, SesEmailService>();  // TODO(SES)

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
                };
            });

        return services;
    }
}