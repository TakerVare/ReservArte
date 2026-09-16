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
            "Guillermo", "Admin", "guille@svalero.com", "Admin1234!", Roles.Admin, "+34600000001");

        var mariaUser = await CreateUserAsync(userManager, orgId,
            "María", "García", "maria.garcia@reservarte.com", "Maria123!", Roles.Employee, "+34600000002");

        var luciaUser = await CreateUserAsync(userManager, orgId,
            "Lucía", "Martínez", "lucia.martinez@reservarte.com", "Lucia123!", Roles.Employee, "+34600000003");

        // ── Empleadas (Id = User.Id, patrón del esquema real) ────────────
        context.Employees.Add(new Employee
        {
            Id = mariaUser.Id,
            OrganizationId = orgId,
            FirstName = mariaUser.FirstName,
            LastName = mariaUser.LastName,
            Email = mariaUser.Email!,
            Phone = mariaUser.PhoneNumber,
            Rol = Roles.Employee,
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
            Rol = Roles.Employee,
            HireDate = new DateOnly(2025, 1, 15),
            IsActive = true,
        });

        await context.SaveChangesAsync();

        // ── Clientas (Id = User.Id; RA-869d7f32r) ────────────────────────
        var carmenUser = await CreateUserAsync(userManager, orgId,
            "Carmen", "López", "carmen.lopez@example.com", "Cliente123!", Roles.Customer, "+34600000004");

        var sofiaUser = await CreateUserAsync(userManager, orgId,
            "Sofía", "Ruiz", "sofia.ruiz@example.com", "Cliente123!", Roles.Customer, "+34600000005");

        context.Customers.Add(NewCustomer(carmenUser, CustomerCategories.Vip));
        context.Customers.Add(NewCustomer(sofiaUser, CustomerCategories.Regular));

        // Tratamiento de datos (obligatorio) para las dos; Carmen acepta además
        // marketing. Ninguna finalidad opcional se da por otorgada sin más.
        var grantedAt = DateTime.UtcNow;
        foreach (var (customer, consentType) in new[]
                 {
                     (carmenUser, CustomerConsentTypes.DataProcessing),
                     (carmenUser, CustomerConsentTypes.Marketing),
                     (sofiaUser, CustomerConsentTypes.DataProcessing),
                 })
        {
            context.CustomerConsents.Add(new CustomerConsent
            {
                OrganizationId = orgId,
                CustomerId = customer.Id,
                ConsentType = consentType,
                IsGranted = true,
                GrantedAt = grantedAt,
            });
        }

        context.CustomerAllergies.Add(new CustomerAllergy
        {
            OrganizationId = orgId,
            CustomerId = carmenUser.Id,
            AllergyDescription = "Látex",
            Severity = AllergySeverities.High,
        });

        context.CustomerNotes.Add(new CustomerNote
        {
            OrganizationId = orgId,
            CustomerId = carmenUser.Id,
            EmployeeId = mariaUser.Id,
            Note = "Prefiere citas por la tarde.",
        });

        await context.SaveChangesAsync();

        // ── Catálogo de servicios (RA-869d7f3z0) ─────────────────────────
        // Centro de cejas: el catálogo demo es el del producto (vol. 1 §3.1.4).
        var cejas = new ServiceCategory
        {
            OrganizationId = orgId,
            Name = "Cejas",
            Description = "Diseño, tinte y mantenimiento de cejas.",
            Color = "#8B5E3C",
            DisplayOrder = 0,
        };

        var pestanas = new ServiceCategory
        {
            OrganizationId = orgId,
            Name = "Pestañas",
            Description = "Lifting y extensiones de pestañas.",
            Color = "#4C3A51",
            DisplayOrder = 1,
        };

        context.ServiceCategories.AddRange(cejas, pestanas);
        await context.SaveChangesAsync();

        var diseno = NewService(orgId, cejas.Id, "Diseño de cejas", 45, 25.00m);
        diseno.Description = "Diseño personalizado con medición y depilación.";

        // El tinte lleva prueba de alergia previa: es el caso que justifica
        // RequiresAllergyTest / AllergyTestHoursBefore en el dominio.
        var tinte = NewService(orgId, cejas.Id, "Tinte de cejas", 30, 18.00m);
        tinte.Description = "Tinte semipermanente.";
        tinte.RequiresAllergyTest = true;

        var lifting = NewService(orgId, pestanas.Id, "Lifting de pestañas", 60, 40.00m);
        lifting.Description = "Curvado y fijación con nutrición.";

        context.Services.AddRange(diseno, tinte, lifting);
        await context.SaveChangesAsync();

        // Variación: modifica precio y duración del servicio base, no los sustituye.
        context.ServiceVariations.Add(new ServiceVariation
        {
            OrganizationId = orgId,
            ServiceId = diseno.Id,
            Name = "Con hilo",
            PriceModifier = 5.00m,
            DurationModifier = 15,
        });

        // Tarifas por nivel: el precio final de cada nivel, no un recargo.
        foreach (var (level, price) in new[]
                 {
                     (EmployeeLevels.Junior, 22.00m),
                     (EmployeeLevels.Senior, 25.00m),
                     (EmployeeLevels.Expert, 30.00m),
                 })
        {
            context.ServicePricings.Add(new ServicePricing
            {
                OrganizationId = orgId,
                ServiceId = diseno.Id,
                EmployeeLevel = level,
                Price = price,
            });
        }

        // Quién sabe hacer qué: lo que permitirá al futuro AvailabilityService
        // ofrecer solo a la empleada capacitada (RA-869d7f4rd).
        foreach (var (employeeId, serviceId, proficiency) in new[]
                 {
                     (mariaUser.Id, diseno.Id, 5),
                     (mariaUser.Id, tinte.Id, 4),
                     (mariaUser.Id, lifting.Id, 3),
                     (luciaUser.Id, diseno.Id, 3),
                     (luciaUser.Id, tinte.Id, 4),
                 })
        {
            context.EmployeeServices.Add(new EmployeeServiceAssignment
            {
                OrganizationId = orgId,
                EmployeeId = employeeId,
                ServiceId = serviceId,
                ProficiencyLevel = proficiency,
            });
        }

        await context.SaveChangesAsync();

        // El admin (adminUser) no tiene fila en Employees: es usuario de
        // gestión, mismo criterio que el seeder original
        _ = adminUser;
    }

    private static Service NewService(
        Guid organizationId, int categoryId, string name, int durationMinutes, decimal basePrice) => new()
        {
            OrganizationId = organizationId,
            CategoryId = categoryId,
            Name = name,
            DurationMinutes = durationMinutes,
            BasePrice = basePrice,
        };

    private static Customer NewCustomer(User user, string category) => new()
    {
        Id = user.Id,
        OrganizationId = user.OrganizationId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email!,
        Phone = user.PhoneNumber,
        Category = category,
        PreferredContactMethod = CustomerContactMethods.Email,
    };

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