using Hangfire;
using Hangfire.PostgreSql;
using ReservArte.API.Options;
using ReservArte.Application.Interfaces;
using ReservArte.Infrastructure.Jobs;

namespace ReservArte.API.Extensions;

public static class BackgroundJobsServiceExtensions
{
    /// <summary>Esquema de PostgreSQL con las tablas de Hangfire, aparte del de la aplicación.</summary>
    public const string StorageSchema = "hangfire";

    /// <summary>
    /// Cola de trabajos en segundo plano (RA-869d7f5zq): Hangfire con sus jobs en
    /// PostgreSQL (H-37 D), para que un recordatorio programado sobreviva a un
    /// reinicio. Fail-fast como MultiTenant: sin proveedor válido la API no
    /// arranca, en vez de aceptar citas cuyos avisos nadie enviará.
    /// </summary>
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(HangfireOptions.SectionName);
        var options = section.Get<HangfireOptions>() ?? new HangfireOptions();
        var isDevelopment = environment.IsDevelopment();

        services.AddOptions<HangfireOptions>()
            .Bind(section)
            .Validate(
                o => IsProvider(o, HangfireOptions.ProviderPostgreSql) || IsProvider(o, HangfireOptions.ProviderNone),
                $"Hangfire:Storage:Provider debe ser {HangfireOptions.ProviderPostgreSql} o " +
                $"{HangfireOptions.ProviderNone} (vacío en appsettings base; configúralo por entorno).")
            .Validate(
                o => isDevelopment || !IsProvider(o, HangfireOptions.ProviderNone),
                $"Hangfire:Storage:Provider {HangfireOptions.ProviderNone} solo se permite en Development.")
            .Validate(o => o.WorkerCount >= 0, "Hangfire:WorkerCount no puede ser negativo.")
            .ValidateOnStart();

        // El job se registra siempre: sin cola no lo dispara nadie, pero los
        // tests lo ejecutan a mano.
        services.AddScoped<ReminderJob>();

        if (!IsProvider(options, HangfireOptions.ProviderPostgreSql))
        {
            services.AddScoped<IReminderJobScheduler, DisabledReminderJobScheduler>();
            return services;
        }

        var connectionString = string.IsNullOrWhiteSpace(options.Storage.ConnectionString)
            ? configuration.GetConnectionString("DefaultConnection")
            : options.Storage.ConnectionString;

        services.AddHangfire(hangfire => hangfire
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                storage => storage.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions { SchemaName = StorageSchema }));

        services.AddHangfireServer(server =>
        {
            if (options.WorkerCount > 0)
            {
                server.WorkerCount = options.WorkerCount;
            }
        });

        services.AddScoped<IReminderJobScheduler, HangfireReminderJobScheduler>();

        return services;
    }

    private static bool IsProvider(HangfireOptions options, string provider) =>
        string.Equals(options.Storage.Provider, provider, StringComparison.OrdinalIgnoreCase);
}
