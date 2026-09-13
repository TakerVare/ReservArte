using Microsoft.EntityFrameworkCore;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Mapping;
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
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(30);
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

        return services;
    }

    /// <summary>
    /// Servicios de aplicación (casos de uso) y sus mapeos. AutoMapper escanea
    /// el ensamblado de Application, donde viven los Profile.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(EmployeeProfile).Assembly));
        services.AddScoped<IEmployeeService, EmployeeService>();

        return services;
    }
}