using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;

namespace ReservArte.IntegrationTests.Infrastructure;

/// <summary>
/// Altas directas en la base para preparar escenarios: cada test crea su propia
/// empleada y su propia clienta, así no depende de lo que hayan dejado otros.
/// </summary>
public static class TestSeed
{
    /// <summary>
    /// Centro nuevo y vacío, para lo que es único por organización (su
    /// configuración): cambiarla en el centro A o en el B alteraría los tests que
    /// comparten la base.
    /// </summary>
    public static async Task<Guid> CreateOrganizationAsync(this ApiFactory factory)
    {
        var id = Guid.NewGuid();
        await using var scope = factory.CreateTenantScope(id);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Organizations.Add(new Organization
        {
            Id = id,
            Name = $"Centro {id:N}",
            Subdomain = $"c{id:N}",
            Email = $"info@{id:N}.test",
        });
        await db.SaveChangesAsync();
        return id;
    }

    /// <summary>
    /// Empleada nueva (con el rol pedido, en la cuenta y en la ficha) y horario de
    /// 09:00 a 18:00 todos los días de la semana.
    /// </summary>
    public static async Task<Employee> CreateEmployeeAsync(
        this ApiFactory factory, Guid organizationId, string rol = Roles.Employee)
    {
        await using var scope = factory.CreateTenantScope(organizationId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await CreateUserAsync(scope, organizationId, "empleada", rol);

        var employee = new Employee
        {
            Id = user.Id,
            OrganizationId = organizationId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            Rol = rol,
        };
        db.Employees.Add(employee);

        for (var day = 0; day <= 6; day++)
        {
            db.EmployeeAvailabilities.Add(new EmployeeAvailability
            {
                OrganizationId = organizationId,
                EmployeeId = employee.Id,
                DayOfWeek = day,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0),
            });
        }

        await db.SaveChangesAsync();
        return employee;
    }

    public static async Task<Customer> CreateCustomerAsync(this ApiFactory factory, Guid organizationId)
    {
        await using var scope = factory.CreateTenantScope(organizationId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await CreateUserAsync(scope, organizationId, "clienta", Roles.Customer);

        var customer = new Customer
        {
            Id = user.Id,
            OrganizationId = organizationId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    /// <summary>
    /// Servicio nuevo del centro, con las variaciones indicadas (nombre, ajuste de
    /// precio y de duración) y asignado a las empleadas que se pasen.
    /// </summary>
    public static async Task<Service> CreateServiceAsync(
        this ApiFactory factory,
        Guid organizationId,
        int durationMinutes,
        decimal basePrice,
        IEnumerable<Employee>? assignedTo = null,
        params (string Name, decimal PriceModifier, int DurationModifier)[] variations)
    {
        await using var scope = factory.CreateTenantScope(organizationId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var service = new Service
        {
            OrganizationId = organizationId,
            Name = $"Servicio {Guid.NewGuid():N}"[..20],
            DurationMinutes = durationMinutes,
            BasePrice = basePrice,
        };
        foreach (var (name, priceModifier, durationModifier) in variations)
        {
            service.Variations.Add(new ServiceVariation
            {
                OrganizationId = organizationId,
                Name = name,
                PriceModifier = priceModifier,
                DurationModifier = durationModifier,
            });
        }

        db.Services.Add(service);
        await db.SaveChangesAsync();

        foreach (var employee in assignedTo ?? [])
        {
            db.EmployeeServices.Add(new EmployeeServiceAssignment
            {
                OrganizationId = organizationId,
                EmployeeId = employee.Id,
                ServiceId = service.Id,
            });
        }

        await db.SaveChangesAsync();
        return service;
    }

    /// <summary>Escenario de citas completo en el centro indicado (ver <see cref="AppointmentScene"/>).</summary>
    public static async Task<AppointmentScene> CreateAppointmentSceneAsync(this ApiFactory factory, Guid organizationId)
    {
        var employee = await factory.CreateEmployeeAsync(organizationId);
        var customer = await factory.CreateCustomerAsync(organizationId);
        var brows = await factory.CreateServiceAsync(organizationId, 45, 25m, [employee], ("Con hilo", 5m, 15));
        var tint = await factory.CreateServiceAsync(organizationId, 30, 20m, [employee]);
        return new AppointmentScene(organizationId, employee, customer, brows, brows.Variations.Single().Id, tint);
    }

    public static async Task<Appointment> CreateAppointmentAsync(
        this ApiFactory factory,
        Guid organizationId,
        Employee employee,
        Customer customer,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        string status = AppointmentStatuses.Pending,
        bool isActive = true)
    {
        await using var scope = factory.CreateTenantScope(organizationId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var appointment = new Appointment
        {
            OrganizationId = organizationId,
            EmployeeId = employee.Id,
            CustomerId = customer.Id,
            AppointmentDate = date,
            StartTime = start,
            EndTime = end,
            Status = status,
            TotalPrice = 25m,
            IsActive = isActive,
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        return appointment;
    }

    private static async Task<User> CreateUserAsync(
        AsyncServiceScope scope, Guid organizationId, string prefix, string rol)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var email = TestData.UniqueEmail(prefix);

        var user = new User
        {
            OrganizationId = organizationId,
            FirstName = prefix,
            LastName = "Prueba",
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Rol = rol,
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo crear {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }
}
