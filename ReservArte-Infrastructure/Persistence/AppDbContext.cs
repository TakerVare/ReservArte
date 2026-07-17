using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence.Configurations;

namespace ReservArte.Infrastructure.Persistence;

public class AppDbContext : IdentityUserContext<User, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Sprint 1: tablas base ─────────────────────────────────────────────
    // El DbSet de Users lo aporta la base IdentityUserContext (AspNetUsers)
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
        modelBuilder.Ignore<EmployeeAvailability>();
        modelBuilder.Ignore<EmployeeException>();
        modelBuilder.Ignore<EmployeeService>();
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
    }
}