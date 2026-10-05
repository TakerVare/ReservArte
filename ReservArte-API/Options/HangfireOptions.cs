namespace ReservArte.API.Options;

/// <summary>Sección <c>Hangfire</c>: la cola de trabajos en segundo plano (RA-869d7f5zq).</summary>
public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    /// <summary>Jobs en la base de datos de la aplicación, en el esquema <c>hangfire</c>.</summary>
    public const string ProviderPostgreSql = "PostgreSql";

    /// <summary>Sin cola: solo en Development (tests y arranques sin base de jobs).</summary>
    public const string ProviderNone = "None";

    /// <summary>Hilos que ejecutan jobs. 0 = el valor por defecto de Hangfire.</summary>
    public int WorkerCount { get; set; }

    public HangfireStorageOptions Storage { get; set; } = new();
}

public class HangfireStorageOptions
{
    /// <summary><see cref="HangfireOptions.ProviderPostgreSql"/> o <see cref="HangfireOptions.ProviderNone"/>.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Vacía = la misma base que la aplicación (<c>ConnectionStrings:DefaultConnection</c>).</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
