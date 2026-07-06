using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReservArte.API.Extensions;
using ReservArte.API.Middleware;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Persistence.Seeders;
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

    // ── ASP.NET Core Identity (AspNetUsers + AspNetUserLogins, sin roles) ─
    builder.Services.AddIdentityServices();

    // ── Multi-tenant: opciones + holder del tenant por petición ──────────
    builder.Services.AddMultiTenancy(builder.Configuration);

    // ── Servicios MVC + documentación OpenAPI (envelope + error.code) ────
    builder.Services.AddControllers();
    builder.Services.AddSwaggerDocumentation();

    // ── Health checks: proceso vivo + smoke test de BD (GET /health) ─────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database");

    var app = builder.Build();

    // ── Enriquecimiento por petición: RequestId + OrganizationId ─────────
    // (debe ir ANTES de UseSerilogRequestLogging para que el evento de
    //  petición completada también lleve ambas propiedades)
    app.UseMiddleware<RequestLogContextMiddleware>();

    // ── Un evento de log estructurado por cada petición HTTP ─────────────
    app.UseSerilogRequestLogging();

    // ── Resolución de tenant (cabecera en dev, subdominio en prod) ───────
    app.UseMiddleware<TenantMiddleware>();

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

    app.UseAuthorization();

    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException se excluye: la lanzan las herramientas
    // "dotnet ef" al construir el host en tiempo de diseño y no es un fallo
    Log.Fatal(ex, "ReservArte API terminó de forma inesperada");
}
finally
{
    Log.CloseAndFlush();
}