using Microsoft.EntityFrameworkCore;
using ReservArte.API.Services;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Persistence.Repositories;
using ReservArte.Infrastructure.Services;

namespace ReservArte.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL (D-28, RA-869f8pmpa). Reintentos ante fallos transitorios de conexión;
        // EfUnitOfWork abre sus transacciones dentro de la estrategia.
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(30);
                }));

        return services;
    }

    /// <summary>
    /// Repositorios de acceso a datos. Scoped como el DbContext: comparten la
    /// unidad de trabajo de la petición.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServicePackageRepository, ServicePackageRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();

        // Transacciones que abarcan ficha y cuenta de Identity (RA-869f1811u).
        // Scoped como el contexto: comparte el AppDbContext con el repositorio
        // y con el UserManager, que es lo que permite abarcarlos a todos.
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Servicios de aplicación (casos de uso). Los mapeos entidad → DTO son clases
    /// estáticas de Mapperly (Application/Mapping) generadas al compilar: no se registran.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Usuario de la petición (claims del JWT) para las reglas que dependen
        // de quién llama. AddHttpContextAccessor es idempotente.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Reloj del sistema. Inyectarlo en vez de llamar a DateTime.UtcNow deja
        // probar sin esperar: la disponibilidad descarta los huecos ya pasados,
        // así que sus tests necesitan fijar qué hora es (RA-869d7f4rd).
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<IServicePackageService, ServicePackageService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IAppointmentService, AppointmentService>();

        return services;
    }
}
