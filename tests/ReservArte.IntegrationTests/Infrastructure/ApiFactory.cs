using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ReservArte.IntegrationTests.Infrastructure;

/// <summary>
/// API entera en memoria contra un PostgreSQL 18 real y desechable (RA-869f6r5ng).
///
/// Un contenedor por ejecución, compartido por todas las clases de la colección
/// <see cref="ApiCollection"/>. La API arranca en Development, como en local, así
/// que al construirse aplica las migraciones y ejecuta <c>DevSeeder</c> (centro A,
/// More Than Brows); después, esta fixture siembra un segundo centro (B) para
/// probar el aislamiento. Los tests crean sus propios datos con emails únicos y
/// no dependen de recuentos globales, porque comparten la base.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18")
        .WithDatabase("reservarte_tests")
        .WithUsername("reservarte")
        .WithPassword(RandomPassword())
        .Build();

    /// <summary>
    /// Clave JWT de esta ejecución. Se fija una vez: las variantes creadas con
    /// <c>WithWebHostBuilder</c> vuelven a pasar por <see cref="ConfigureWebHost"/> y deben
    /// aceptar los mismos tokens.
    /// </summary>
    private readonly string _jwtSecret = RandomPassword() + RandomPassword();

    /// <summary>Tokens de acceso por cuenta y centro (ver <see cref="ApiClient.LoginAsync"/>).</summary>
    public ConcurrentDictionary<string, string> Tokens { get; } = new();

    /// <summary>Correos que la API habría enviado: sustituye al proveedor de archivo.</summary>
    public CapturingEmailService Emails { get; } = new();

    /// <summary>Avisos que la API habría puesto en la cola de Hangfire, con su hora.</summary>
    public CapturingReminderJobScheduler ReminderJobs { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Construir el servidor dispara las migraciones y DevSeeder (Program.cs, Development).
        try
        {
            _ = Server;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("without ever building an IHost"))
        {
            // Un fallo antes de builder.Build() lo registra el catch de Program.cs como
            // FTL y no lo propaga (sale con código 1, RA-869f6r5jf), así que aquí solo
            // llega este aviso genérico. El motivo real está en la salida estándar.
            throw new InvalidOperationException(
                "La API no arrancó. Program.cs registra el motivo como [FTL] en la salida estándar; " +
                "vuelve a ejecutar con: dotnet test tests/ReservArte.IntegrationTests " +
                "--logger \"console;verbosity=detailed\"",
                ex);
        }

        await SeedOrganizationBAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development: la resolución del tenant por cabecera solo se admite ahí
        // (RA-869f6r5jf), y es la que usa la SPA en local.
        builder.UseEnvironment("Development");

        // Configuración explícita y completa para que el resultado no dependa de los
        // User Secrets del equipo que ejecuta (en el CI no hay) y nunca toque la
        // base de desarrollo. Las credenciales sociales vacías desactivan esos esquemas.
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SecretKey", _jwtSecret);
        builder.UseSetting("Authentication:Google:ClientId", string.Empty);
        builder.UseSetting("Authentication:Google:ClientSecret", string.Empty);
        builder.UseSetting("Authentication:Meta:AppId", string.Empty);
        builder.UseSetting("Authentication:Meta:AppSecret", string.Empty);
        builder.UseSetting("Email:Provider", "File");
        // Sin Hangfire: un servidor de jobs disparando en mitad de los tests los
        // haría depender del reloj. Lo que se programa se captura (ReminderJobs).
        builder.UseSetting("Hangfire:Storage:Provider", "None");
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.UseSetting("Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command", "Warning");
        builder.UseSetting("Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime", "Warning");

        builder.ConfigureTestServices(services =>
        {
            // Los correos se capturan en memoria en vez de escribirse en ./sent-emails/.
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);

            services.RemoveAll<IReminderJobScheduler>();
            services.AddSingleton<IReminderJobScheduler>(ReminderJobs);
        });
    }

    /// <summary>
    /// Ámbito de servicios con el tenant ya fijado, como lo dejaría el
    /// <c>TenantMiddleware</c> en una petición. Sirve para usar repositorios y
    /// servicios de la API directamente contra PostgreSQL.
    /// </summary>
    public AsyncServiceScope CreateTenantScope(Guid organizationId)
    {
        var scope = Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ICurrentOrganizationService>().SetOrganization(organizationId);
        return scope;
    }

    /// <summary>
    /// Centro B con una administradora y una clienta. El tenant se fija antes de
    /// usar Identity: sin él, la búsqueda por email no queda acotada al centro.
    /// </summary>
    private async Task SeedOrganizationBAsync()
    {
        await using var scope = CreateTenantScope(TestData.OrgB);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        db.Organizations.Add(new Organization
        {
            Id = TestData.OrgB,
            Name = "Centro B",
            Subdomain = "centrob",
            Email = "info@centrob.test",
        });
        await db.SaveChangesAsync();

        await CreateUserAsync(userManager, TestData.OrgB, TestData.AdminB, Roles.Admin);
        var customer = await CreateUserAsync(userManager, TestData.OrgB, TestData.CustomerB, Roles.Customer);

        db.Customers.Add(new Customer
        {
            Id = customer.Id,
            OrganizationId = TestData.OrgB,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email!,
        });
        await db.SaveChangesAsync();
    }

    private static async Task<User> CreateUserAsync(
        UserManager<User> userManager, Guid organizationId, TestAccount account, string rol)
    {
        var user = new User
        {
            OrganizationId = organizationId,
            FirstName = account.FirstName,
            LastName = "Prueba",
            UserName = account.Email,
            Email = account.Email,
            EmailConfirmed = true,
            Rol = rol,
        };

        var result = await userManager.CreateAsync(user, account.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo crear {account.Email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    private static string RandomPassword() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
}
