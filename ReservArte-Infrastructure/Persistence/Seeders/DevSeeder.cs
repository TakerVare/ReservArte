using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Seeders;

public static class DevSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Idempotente: no hace nada si la organización ya existe
        if (await context.Organizations.AnyAsync())
            return;

        var orgId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var org = new Organization
        {
            Id = orgId,
            Name = "More Than Brows",
            Subdomain = "morethanbrows",
            Email = "info@morethanbrows.com",
            Phone = "+34600000000",
            Country = "ES",
            IsActive = true,
        };
        context.Organizations.Add(org);

        // ── Usuarios ─────────────────────────────────────────────────────
        // Contraseñas en texto plano SOLO para dev.
        // TODO: sustituir por BCrypt.Net.BCrypt.HashPassword("...", 12)
        //       cuando se instale BCrypt.Net-Next (tarea de autenticación).
        var adminUser = new User
        {
            OrganizationId = orgId,
            FirstName = "Guillermo",
            LastName = "Admin",
            Email = "guille@svalero.com",
            Password = "Admin1234!",
            Rol = "admin",
            Phone = "+34600000001",
        };
        context.Users.Add(adminUser);

        var mariaUser = new User
        {
            OrganizationId = orgId,
            FirstName = "María",
            LastName = "García",
            Email = "maria.garcia@reservarte.com",
            Password = "Maria123!",
            Rol = "employee",
            Phone = "+34600000002",
        };
        context.Users.Add(mariaUser);

        var luciaUser = new User
        {
            OrganizationId = orgId,
            FirstName = "Lucía",
            LastName = "Martínez",
            Email = "lucia.martinez@reservarte.com",
            Password = "Lucia123!",
            Rol = "employee",
            Phone = "+34600000003",
        };
        context.Users.Add(luciaUser);

        // Primer guardado: genera los Id INT IDENTITY de los usuarios,
        // necesarios para crear los Employees (Id = User.Id)
        await context.SaveChangesAsync();

        // ── Empleadas (Id = User.Id, patrón del esquema real) ────────────
        context.Employees.Add(new Employee
        {
            Id = mariaUser.Id,
            OrganizationId = orgId,
            FirstName = mariaUser.FirstName,
            LastName = mariaUser.LastName,
            Email = mariaUser.Email,
            Phone = mariaUser.Phone,
            Rol = "employee",
            HireDate = new DateOnly(2024, 3, 1),
            IsActive = true,
        });

        context.Employees.Add(new Employee
        {
            Id = luciaUser.Id,
            OrganizationId = orgId,
            FirstName = luciaUser.FirstName,
            LastName = luciaUser.LastName,
            Email = luciaUser.Email,
            Phone = luciaUser.Phone,
            Rol = "employee",
            HireDate = new DateOnly(2025, 1, 15),
            IsActive = true,
        });

        await context.SaveChangesAsync();
    }
}