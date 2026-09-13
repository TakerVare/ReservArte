using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence.Configurations;

namespace ReservArte.Infrastructure.Persistence;

public class AppDbContext : IdentityUserContext<User, int>
{
    private readonly ICurrentOrganizationService? _currentOrganization;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Constructor usado en ejecución: recibe el tenant resuelto por
    /// TenantMiddleware para alimentar los query filters globales. El otro
    /// constructor queda para escenarios sin petición (migraciones, seeders y
    /// tests), donde no hay organización y los filtros dejan pasar todo.
    /// </summary>
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentOrganizationService currentOrganization) : base(options)
    {
        _currentOrganization = currentOrganization;
    }

    /// <summary>
    /// Tenant activo, o null fuera de una petición. Se lee dentro de los query
    /// filters, por lo que EF lo traduce a un parámetro evaluado en CADA
    /// consulta, no una sola vez al construir el modelo.
    /// </summary>
    private Guid? CurrentOrganizationId => _currentOrganization?.OrganizationId;

    // ── Sprint 1: tablas base ─────────────────────────────────────────────
    // El DbSet de Users lo aporta la base IdentityUserContext (AspNetUsers)
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmployeeAvailability> EmployeeAvailabilities => Set<EmployeeAvailability>();
    public DbSet<EmployeeException> EmployeeExceptions => Set<EmployeeException>();

    // TODO Sprint 2: Customers, Services, Appointments, Payments, ...
    // TODO Sprint 3: Reminders, Photos, WaitingList, ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mapea AspNetUsers, AspNetUserClaims, AspNetUserLogins y
        // AspNetUserTokens — la variante SIN roles de Identity, acorde
        // al alcance de la tarea (AspNetUsers + AspNetUserLogins)
        base.OnModelCreating(modelBuilder);

        // Sprint 1: ignorar todas las entidades fuera de scope.
        // EF Core las descubriría por navegaciones; se irán retirando de esta
        // lista a medida que entren en migraciones de sprints posteriores.
        modelBuilder.Ignore<Customer>();
        modelBuilder.Ignore<CustomerNote>();
        modelBuilder.Ignore<CustomerAllergy>();
        modelBuilder.Ignore<CustomerConsent>();
        modelBuilder.Ignore<CustomerPaymentMethod>();
        modelBuilder.Ignore<Appointment>();
        modelBuilder.Ignore<AppointmentServiceItem>();
        modelBuilder.Ignore<Service>();
        modelBuilder.Ignore<ServiceCategory>();
        modelBuilder.Ignore<ServiceVariation>();
        modelBuilder.Ignore<ServicePricing>();
        modelBuilder.Ignore<ServicePackage>();
        modelBuilder.Ignore<ServicePackageItem>();
        modelBuilder.Ignore<ServicePromotion>();
        modelBuilder.Ignore<ServiceProduct>();
        modelBuilder.Ignore<ServicePhoto>();
        modelBuilder.Ignore<Payment>();
        modelBuilder.Ignore<WaitingList>();
        modelBuilder.Ignore<EmployeeServiceAssignment>();
        modelBuilder.Ignore<MessageTemplate>();
        modelBuilder.Ignore<ReminderConfiguration>();
        modelBuilder.Ignore<ReminderLog>();
        modelBuilder.Ignore<ConfirmationToken>();
        modelBuilder.Ignore<CancellationPolicy>();
        modelBuilder.Ignore<Configuration>();
        modelBuilder.Ignore<Product>();
        modelBuilder.Ignore<ProductCategory>();
        modelBuilder.Ignore<ProductSale>();
        modelBuilder.Ignore<ProductSaleItem>();
        modelBuilder.Ignore<InventoryMovement>();

        // Sprint 1: solo las configuraciones de las tablas base
        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeAvailabilityConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeExceptionConfiguration());

        // Aislamiento multi-tenant (RA-869f17myx): sin este filtro, una consulta
        // directa a estas tablas devolvería filas de TODAS las organizaciones,
        // porque su pertenencia al tenant solo se deducía de la FK a Employee.
        // Fuera de una petición (migraciones, seeders) no hay organización
        // resuelta y el filtro no restringe nada.
        modelBuilder.Entity<EmployeeAvailability>().HasQueryFilter(
            a => CurrentOrganizationId == null || a.OrganizationId == CurrentOrganizationId);

        modelBuilder.Entity<EmployeeException>().HasQueryFilter(
            e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);
    }
}