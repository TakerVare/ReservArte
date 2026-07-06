using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Seeders;

public static class DevSeeder
{
    public static async Task SeedAsync(AppDbContext context, UserManager<User> userManager)
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
        await context.SaveChangesAsync();

        // ── Usuarios vía Identity: CreateAsync hashea la contraseña
        //    (PasswordHash), normaliza email/username y genera SecurityStamp ──
        var adminUser = await CreateUserAsync(userManager, orgId,
            "Guillermo", "Admin", "guille@svalero.com", "Admin1234!", "admin", "+34600000001");

        var mariaUser = await CreateUserAsync(userManager, orgId,
            "María", "García", "maria.garcia@reservarte.com", "Maria123!", "employee", "+34600000002");

        var luciaUser = await CreateUserAsync(userManager, orgId,
            "Lucía", "Martínez", "lucia.martinez@reservarte.com", "Lucia123!", "employee", "+34600000003");

        // ── Empleadas (Id = User.Id, patrón del esquema real) ────────────
        context.Employees.Add(new Employee
        {
            Id = mariaUser.Id,
            OrganizationId = orgId,
            FirstName = mariaUser.FirstName,
            LastName = mariaUser.LastName,
            Email = mariaUser.Email!,
            Phone = mariaUser.PhoneNumber,
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
            Email = luciaUser.Email!,
            Phone = luciaUser.PhoneNumber,
            Rol = "employee",
            HireDate = new DateOnly(2025, 1, 15),
            IsActive = true,
        });

        await context.SaveChangesAsync();

        // El admin (adminUser) no tiene fila en Employees: es usuario de
        // gestión, mismo criterio que el seeder original
        _ = adminUser;
    }

    private static async Task<User> CreateUserAsync(
        UserManager<User> userManager,
        Guid organizationId,
        string firstName,
        string lastName,
        string email,
        string password,
        string rol,
        string phone)
    {
        var user = new User
        {
            OrganizationId = organizationId,
            FirstName = firstName,
            LastName = lastName,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PhoneNumber = phone,
            Rol = rol,
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"DevSeeder: no se pudo crear el usuario {email}: {errors}");
        }

        return user;
    }
}