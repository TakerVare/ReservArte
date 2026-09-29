using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReservArte.API.Extensions;
using ReservArte.API.Middleware;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Persistence.Seeders;
using ReservArte.Shared.Json;
using Serilog;

// ── Bootstrap logger: captura errores del propio arranque, antes de que
//    exista la configuración completa (patrón de dos fases de Serilog) ────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando ReservArte API");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog definitivo: lee la sección "Serilog" de appsettings ──────
    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));

    // ── Base de datos ─────────────────────────────────────────────────────
    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddRepositories();
    builder.Services.AddApplicationServices();

    // ── ASP.NET Core Identity (AspNetUsers + AspNetUserLogins, sin roles) ─
    builder.Services.AddIdentityServices();

    // ── Emisor de tokens JWT (sección "Jwt" + IJwtTokenService) ──────────
    builder.Services.AddJwtAuthentication(builder.Configuration);

    // ── Rate limiting de endpoints de auth (vol. 1 §4.4.3) ───────────────
    builder.Services.AddRateLimiting();

    // ── Login social: cookie externa + Google/Apple si hay credenciales ──
    builder.Services.AddExternalAuthentication(builder.Configuration);

    // ── Multi-tenant: opciones + holder del tenant por petición ──────────
    builder.Services.AddMultiTenancy(builder.Configuration, builder.Environment);

    // ── CORS para la SPA (sección "Cors:AllowedOrigins") ─────────────────
    builder.Services.AddCorsPolicy(builder.Configuration);

    // ── Servicios MVC + documentación OpenAPI (envelope + error.code) ────
    // Fechas con hora siempre en UTC en la frontera JSON (RA-869f8pmnm)
    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()))
        // 400 de model binding con envelope, no ProblemDetails (RA-869f1k17q)
        .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
    builder.Services.AddSwaggerDocumentation();

    // ── Health checks: proceso vivo + smoke test de BD (GET /health) ─────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database");

    // ── Excepciones no controladas: 500 GEN_INTERNAL_ERROR con envelope ──
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    var app = builder.Build();

    // ── Enriquecimiento por petición: RequestId + OrganizationId ─────────
    // (debe ir ANTES de UseSerilogRequestLogging para que el evento de
    //  petición completada también lleve ambas propiedades)
    app.UseMiddleware<RequestLogContextMiddleware>();

    // ── Un evento de log estructurado por cada petición HTTP ─────────────
    app.UseSerilogRequestLogging();

    // ── Excepciones no controladas (RA-869f74u70): dentro del log de petición,
    //    para que el evento "responded" registre el 500, y antes de todo lo
    //    demás, para cubrir también el middleware de tenant y la autenticación.
    //    La lambda vacía solo satisface la configuración: responde el
    //    GlobalExceptionHandler registrado arriba.
    app.UseExceptionHandler(_ => { });

    // ── 404 de ruta inexistente y 405 con envelope (RA-869f1k17q): solo bajo /api
    //    y solo si la respuesta sale vacía; los 404 de los controladores ya
    //    llevan su cuerpo y no pasan por aquí.
    app.UseStatusCodePages(ApiStatusCodePages.WriteAsync);

    // Solo en Development: Swagger + migraciones + seed automáticos
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        await db.Database.MigrateAsync();
        await DevSeeder.SeedAsync(db, userManager);
    }
    else
    {
        // En dev la API corre solo en HTTP; la redirección https aplica
        // fuera de Development (config completa HTTPS/HSTS: vol. 2 §9.1.1,
        // tareas de seguridad/infra)
        app.UseHttpsRedirection();
    }

    // CORS antes de Authentication/Authorization: el preflight OPTIONS debe
    // resolverse sin pasar por el pipeline de auth.
    app.UseCors(CorsServiceExtensions.DefaultPolicy);

    // Autenticación ANTES del TenantMiddleware: éste comprueba la coherencia
    // entre el claim organization_id del JWT y el tenant resuelto, así que
    // necesita el usuario ya autenticado. La resolución externa (cookie
    // OAuth) también se materializa aquí.
    app.UseAuthentication();

    // ── Resolución de tenant (cabecera en dev, subdominio en prod) ───────
    app.UseMiddleware<TenantMiddleware>();

    app.UseAuthorization();

    // Rate limiting: tras la autorización, antes del enrutado a controladores
    app.UseRateLimiter();

    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException se excluye: la lanzan las herramientas
    // "dotnet ef" al construir el host en tiempo de diseño y no es un fallo
    Log.Fatal(ex, "ReservArte API terminó de forma inesperada");

    // Código de salida distinto de 0: sin él, un arranque fallido (configuración
    // inválida, BD inaccesible) terminaría con 0 y un orquestador lo tomaría por
    // una parada limpia. La parada normal (Ctrl+C, SIGTERM) no pasa por aquí.
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
