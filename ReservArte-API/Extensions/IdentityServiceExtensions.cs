using Microsoft.AspNetCore.Identity;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;

namespace ReservArte.API.Extensions;

public static class IdentityServiceExtensions
{
    /// <summary>
    /// Registra ASP.NET Core Identity en su variante core (sin roles ni
    /// cookies): UserManager, PasswordHasher, stores EF sobre AppDbContext
    /// y los token providers que usarán reset de contraseña y 2FA TOTP.
    /// La autenticación de la API será JwtBearer (siguientes tareas).
    /// </summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        // Requerido por AddDefaultTokenProviders(): los token providers de
        // Identity (reset de contraseña, 2FA) cifran sus tokens con
        // IDataProtectionProvider. En producción, la persistencia del
        // key ring se configurará con las tareas de infra (vol. 2 §9.1.1).
        services.AddDataProtection();

        services
            .AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Política de contraseñas: mínimo 8 + defaults de Identity
                // (mayúscula, minúscula, dígito y símbolo). Se ajustará si
                // la tarea de endpoints de auth fija otra política.
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}