using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Usuarios ──────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();

    // ── Servicios ─────────────────────────────────────────────────────────
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceVariation> ServiceVariations => Set<ServiceVariation>();
    public DbSet<ServicePricing> ServicePricings => Set<ServicePricing>();
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
    public DbSet<ServicePackageItem> ServicePackageItems => Set<ServicePackageItem>();
    public DbSet<ServicePromotion> ServicePromotions => Set<ServicePromotion>();

    // ── Productos ─────────────────────────────────────────────────────────
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ServiceProduct> ServiceProducts => Set<ServiceProduct>();
    public DbSet<ProductSale> ProductSales => Set<ProductSale>();
    public DbSet<ProductSaleItem> ProductSaleItems => Set<ProductSaleItem>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    // ── Empleados ─────────────────────────────────────────────────────────
    public DbSet<EmployeeAvailability> EmployeeAvailabilities => Set<EmployeeAvailability>();
    public DbSet<EmployeeException> EmployeeExceptions => Set<EmployeeException>();
    public DbSet<EmployeeService> EmployeeServices => Set<EmployeeService>();

    // ── Citas ─────────────────────────────────────────────────────────────
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentServiceItem> AppointmentServiceItems => Set<AppointmentServiceItem>();
    public DbSet<WaitingList> WaitingLists => Set<WaitingList>();

    // ── Pagos ─────────────────────────────────────────────────────────────
    public DbSet<CustomerPaymentMethod> CustomerPaymentMethods => Set<CustomerPaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();

    // ── Clientes (aux) ────────────────────────────────────────────────────
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();
    public DbSet<CustomerAllergy> CustomerAllergies => Set<CustomerAllergy>();
    public DbSet<CustomerConsent> CustomerConsents => Set<CustomerConsent>();

    // ── Recordatorios y tokens ────────────────────────────────────────────
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<ReminderConfiguration> ReminderConfigurations => Set<ReminderConfiguration>();
    public DbSet<ReminderLog> ReminderLogs => Set<ReminderLog>();
    public DbSet<ConfirmationToken> ConfirmationTokens => Set<ConfirmationToken>();

    // ── Fotos y políticas ─────────────────────────────────────────────────
    public DbSet<ServicePhoto> ServicePhotos => Set<ServicePhoto>();
    public DbSet<CancellationPolicy> CancellationPolicies => Set<CancellationPolicy>();

    // ── Configuración (singleton) ─────────────────────────────────────────
    public DbSet<Configuration> Configuration => Set<Configuration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Aplicar todas las configuraciones de entidad ──────────────────
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ── Query filters globales por IsActive ───────────────────────────
        // Solo se aplican a entidades que tienen la columna IsActive.
        // Para consultas que necesiten incluir inactivos: .IgnoreQueryFilters()

        modelBuilder.Entity<Customer>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<Employee>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServiceCategory>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<Service>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServiceVariation>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServicePricing>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServicePackage>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServicePackageItem>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServicePromotion>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ProductCategory>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<Product>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServiceProduct>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<EmployeeAvailability>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<EmployeeException>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<EmployeeService>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<CustomerPaymentMethod>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<Payment>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<CustomerNote>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<CustomerAllergy>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<CustomerConsent>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<MessageTemplate>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ReminderConfiguration>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<WaitingList>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServicePhoto>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<CancellationPolicy>().HasQueryFilter(e => e.IsActive);

        // ── Entidades SIN query filter ────────────────────────────────────
        // User, AppointmentServiceItem, ProductSale, ProductSaleItem,
        // InventoryMovement, ReminderLog, ConfirmationToken, Configuration
        // → no tienen IsActive o son registros inmutables de auditoría/log
    }
}
