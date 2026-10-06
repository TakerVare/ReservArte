using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Seeders;

public static class DevSeeder
{
    private static readonly Guid PilotOrganizationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    /// <summary>
    /// Administradores que entran solo con Google (sin contraseña local): la API
    /// los vincula por email en su primer login social. Se aseguran también en
    /// bases ya sembradas, para que los dos equipos los tengan al arrancar la API
    /// (RA-869faedz3). El nombre es el del perfil de Google.
    /// </summary>
    private static readonly (string FirstName, string LastName, string Email)[] GoogleAdmins =
    [
        ("Taker", "Vare", "takervare@gmail.com"),
    ];

    public static async Task SeedAsync(AppDbContext context, UserManager<User> userManager)
    {
        // Sin petición no hay tenant y los filtros están cerrados (869f6r5vy): el
        // seeder siembra y comprueba datos del centro piloto y de cuentas que aún
        // no tienen organización resuelta, así que trabaja en el ámbito de sistema.
        // UserManager comparte este contexto (mismo scope), y también lo ve.
        using var systemScope = context.EnterSystemScope("DevSeeder: siembra del centro piloto sin petición");

        // Idempotente: la organización y sus datos, solo si no existe ninguna
        if (!await context.Organizations.AnyAsync())
            await SeedPilotOrganizationAsync(context, userManager);

        await EnsurePilotSettingsAsync(context);
        await EnsurePilotRemindersAsync(context);
        await EnsureGoogleAdminsAsync(context, userManager);
        await EnsureAdminEmployeesAsync(context);
        await EnsureBookingDemoAsync(context);
    }

    /// <summary>
    /// Configuración del centro piloto (RA-869f74u7y), con los valores por defecto
    /// escritos en su fila, como `data/demo`. También en bases ya sembradas; no
    /// toca la que ya exista.
    /// </summary>
    private static async Task EnsurePilotSettingsAsync(AppDbContext context)
    {
        if (!await context.Organizations.AnyAsync(o => o.Id == PilotOrganizationId)
            || await context.OrganizationSettings.AnyAsync(s => s.OrganizationId == PilotOrganizationId))
        {
            return;
        }

        context.OrganizationSettings.Add(new OrganizationSettings { OrganizationId = PilotOrganizationId });
        await context.SaveChangesAsync();
    }

    /// <summary>Nombre de la plantilla de recordatorio que siembran el seeder y `data/demo`.</summary>
    public const string PilotReminderTemplateName = "Recordatorio de cita";

    /// <summary>Asunto y texto de esa plantilla, con las variables que sustituirá el envío.</summary>
    public const string PilotReminderSubject = "Recordatorio de tu cita en {{organizationName}}";

    public const string PilotReminderBody =
        "Hola, {{customerName}}:\n\nTe recordamos tu cita en {{organizationName}} el {{appointmentDate}} "
        + "a las {{appointmentTime}} con {{employeeName}}.\n\nSi no puedes venir, avísanos con antelación. "
        + "¡Te esperamos!";

    /// <summary>
    /// Recordatorio por defecto del centro piloto (RA-869d7f5zq): un email 24 horas
    /// antes de la cita, entre las 09:00 y las 21:00, con su plantilla. Igual que
    /// `data/demo`. También en bases ya sembradas; si el centro ya tiene algún
    /// recordatorio o esa plantilla, no toca nada.
    /// </summary>
    private static async Task EnsurePilotRemindersAsync(AppDbContext context)
    {
        if (!await context.Organizations.AnyAsync(o => o.Id == PilotOrganizationId)
            || await context.ReminderConfigurations.AnyAsync(r => r.OrganizationId == PilotOrganizationId)
            || await context.MessageTemplates.AnyAsync(
                m => m.OrganizationId == PilotOrganizationId && m.Name == PilotReminderTemplateName))
        {
            return;
        }

        context.ReminderConfigurations.Add(new ReminderConfiguration
        {
            OrganizationId = PilotOrganizationId,
            ReminderOrder = 1,
            HoursBeforeAppointment = 24,
            Channel = ReminderChannels.Email,
            AllowedSendStartTime = new TimeOnly(9, 0),
            AllowedSendEndTime = new TimeOnly(21, 0),
            MessageTemplate = new MessageTemplate
            {
                OrganizationId = PilotOrganizationId,
                Name = PilotReminderTemplateName,
                Type = MessageTemplateTypes.EmailReminder,
                Subject = PilotReminderSubject,
                Body = PilotReminderBody,
            },
        });
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Servicios de ejemplo de la pantalla de reserva (RA-869fagpx9): nombre,
    /// categoría, minutos, precio y quién lo presta.
    /// </summary>
    private static readonly (string Name, string Category, int Minutes, decimal Price, string[] Employees)[] BookingDemoServices =
    [
        ("Laminado de cejas", "Cejas", 60, 45.00m, ["maria.garcia@reservarte.com"]),
        ("Henna de cejas", "Cejas", 40, 22.00m, ["maria.garcia@reservarte.com", "lucia.martinez@reservarte.com"]),
        ("Tinte de pestañas", "Pestañas", 20, 15.00m, ["lucia.martinez@reservarte.com"]),
    ];

    /// <summary>
    /// Horario semanal de las empleadas demo (0 = lunes), el mismo que siembra
    /// `data/demo`: sin horario no hay huecos que reservar.
    /// </summary>
    private static readonly (string Email, (int Day, TimeOnly Start, TimeOnly End)[] Week)[] BookingDemoSchedules =
    [
        ("maria.garcia@reservarte.com",
            [(0, new(9, 0), new(18, 0)), (1, new(9, 0), new(18, 0)), (2, new(9, 0), new(18, 0)),
             (3, new(9, 0), new(18, 0)), (4, new(9, 0), new(14, 0))]),
        ("lucia.martinez@reservarte.com",
            [(0, new(10, 0), new(19, 0)), (1, new(10, 0), new(19, 0)), (2, new(10, 0), new(19, 0)),
             (3, new(10, 0), new(19, 0)), (4, new(10, 0), new(15, 0))]),
    ];

    /// <summary>
    /// Deja la base del piloto lista para reservar (RA-869fagpx9): los servicios de
    /// ejemplo con sus asignaciones y el horario de las empleadas demo que no tengan
    /// ninguno. Idempotente y también en bases ya sembradas, como los admins de
    /// Google; no toca lo que ya existe.
    /// </summary>
    private static async Task EnsureBookingDemoAsync(AppDbContext context)
    {
        if (!await context.Organizations.AnyAsync(o => o.Id == PilotOrganizationId))
            return;

        // En el ámbito de sistema los filtros no restringen: se acota a mano.
        var employees = await context.Employees
            .Where(e => e.OrganizationId == PilotOrganizationId && e.IsActive)
            .ToDictionaryAsync(e => e.Email);
        var categories = await context.ServiceCategories
            .Where(c => c.OrganizationId == PilotOrganizationId)
            .ToListAsync();

        foreach (var (name, categoryName, minutes, price, providers) in BookingDemoServices)
        {
            var category = categories.FirstOrDefault(c => c.Name == categoryName);
            if (category is null)
                continue;

            var service = await context.Services
                .FirstOrDefaultAsync(sv => sv.OrganizationId == PilotOrganizationId && sv.Name == name);
            if (service is null)
            {
                service = NewService(PilotOrganizationId, category.Id, name, minutes, price);
                context.Services.Add(service);
                await context.SaveChangesAsync();
            }

            foreach (var email in providers)
            {
                if (!employees.TryGetValue(email, out var employee))
                    continue;

                var serviceId = service.Id;
                var assigned = await context.EmployeeServices.AnyAsync(a =>
                    a.OrganizationId == PilotOrganizationId && a.EmployeeId == employee.Id && a.ServiceId == serviceId);
                if (!assigned)
                {
                    context.EmployeeServices.Add(new EmployeeServiceAssignment
                    {
                        OrganizationId = PilotOrganizationId,
                        EmployeeId = employee.Id,
                        ServiceId = serviceId,
                        ProficiencyLevel = 3,
                    });
                }
            }
        }

        foreach (var (email, week) in BookingDemoSchedules)
        {
            if (!employees.TryGetValue(email, out var employee))
                continue;

            var hasSchedule = await context.EmployeeAvailabilities
                .AnyAsync(a => a.EmployeeId == employee.Id && a.IsActive);
            if (hasSchedule)
                continue;

            foreach (var (day, start, end) in week)
            {
                context.EmployeeAvailabilities.Add(new EmployeeAvailability
                {
                    OrganizationId = PilotOrganizationId,
                    EmployeeId = employee.Id,
                    DayOfWeek = day,
                    StartTime = start,
                    EndTime = end,
                    IsRecurring = true,
                });
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Admins que además tienen ficha de empleado (4.2b, decisión de Guillermo): sin
    /// ella no firman notas de clientas. Sin horario ni servicios, así que no salen
    /// en la reserva.
    /// </summary>
    private static readonly string[] AdminsWithEmployeeRecord = ["guille@svalero.com"];

    private static async Task EnsureAdminEmployeesAsync(AppDbContext context)
    {
        if (!await context.Organizations.AnyAsync(o => o.Id == PilotOrganizationId))
            return;

        foreach (var email in AdminsWithEmployeeRecord)
        {
            // En el ámbito de sistema, se acota a mano por la organización del piloto.
            var user = await context.Users.SingleOrDefaultAsync(u =>
                u.OrganizationId == PilotOrganizationId && u.NormalizedEmail == email.ToUpperInvariant());
            if (user is null)
                continue;

            var hasRecord = await context.Employees.AnyAsync(e => e.Id == user.Id);
            if (hasRecord)
                continue;

            context.Employees.Add(new Employee
            {
                Id = user.Id,
                OrganizationId = PilotOrganizationId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!.Trim().ToLowerInvariant(),
                Phone = user.PhoneNumber,
                Rol = user.Rol,
                IsActive = true,
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureGoogleAdminsAsync(AppDbContext context, UserManager<User> userManager)
    {
        // Solo en la base del piloto: si alguien sembró otra organización a mano, no se toca.
        if (!await context.Organizations.AnyAsync(o => o.Id == PilotOrganizationId))
            return;

        foreach (var (firstName, lastName, email) in GoogleAdmins)
        {
            // En el ámbito de sistema el filtro de usuarios no restringe; se acota a
            // mano por organización (ver AppDbContext, filtro de User).
            var normalizedEmail = userManager.NormalizeEmail(email);
            var existing = await context.Users.SingleOrDefaultAsync(u =>
                u.OrganizationId == PilotOrganizationId && u.NormalizedEmail == normalizedEmail);

            if (existing is null)
            {
                await CreateUserAsync(userManager, PilotOrganizationId,
                    firstName, lastName, email, password: null, Roles.Admin, phone: null);
                continue;
            }

            await PromoteToAdminAsync(context, existing);
        }
    }

    /// <summary>
    /// Una cuenta que ya entró con Google antes de esto es una clienta (el alta
    /// social crea cuenta, vínculo y ficha). Pasa a Admin conservando el vínculo,
    /// y su ficha de clienta queda de baja lógica para que no salga en la lista
    /// de clientas; no se borra nada (decisión de Guillermo, RA-869faedz3).
    /// </summary>
    private static async Task PromoteToAdminAsync(AppDbContext context, User user)
    {
        var changed = false;

        if (user.Rol != Roles.Admin)
        {
            user.Rol = Roles.Admin;
            user.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        var customer = await context.Customers
            .SingleOrDefaultAsync(c => c.Id == user.Id && c.IsActive);
        if (customer is not null)
        {
            customer.IsActive = false;
            changed = true;
        }

        if (changed)
            await context.SaveChangesAsync();
    }

    private static async Task SeedPilotOrganizationAsync(AppDbContext context, UserManager<User> userManager)
    {
        var orgId = PilotOrganizationId;

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
        string? password,
        string rol,
        string? phone)
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

        // Sin contraseña: cuenta solo social (PasswordHash NULL), como el alta por Google.
        var result = password is null
            ? await userManager.CreateAsync(user)
            : await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"DevSeeder: no se pudo crear el usuario {email}: {errors}");
        }

        return user;
    }
}
