using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly int _organizationId;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentOrganizationService currentOrg)
        : base(options)
    {
        _organizationId = currentOrg.OrganizationId;
    }

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

    // ── Configuración ─────────────────────────────────────────────────────
    public DbSet<Configuration> Configuration => Set<Configuration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ── Query filters por OrganizationId e IsActive ───────────────────
        // _organizationId se resuelve una vez al construir el DbContext (scope HTTP).
        // Para saltarse los filtros en un query concreto: .IgnoreQueryFilters()

        // Entidades raíz: filtro por organización + soft-delete
        modelBuilder.Entity<Customer>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<Employee>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<ServiceCategory>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<Service>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<ServicePackage>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<ProductCategory>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<Product>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<Payment>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<MessageTemplate>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<ReminderConfiguration>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<WaitingList>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);
        modelBuilder.Entity<CancellationPolicy>().HasQueryFilter(e => e.OrganizationId == _organizationId && e.IsActive);

        // Entidades raíz: solo filtro por organización (sin IsActive)
        modelBuilder.Entity<User>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<ProductSale>().HasQueryFilter(e => e.OrganizationId == _organizationId);
        modelBuilder.Entity<Configuration>().HasQueryFilter(e => e.OrganizationId == _organizationId);

        // Entidades hijas: solo soft-delete (la org se garantiza por FK al padre)
        modelBuilder.Entity<ServiceVariation>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServicePricing>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServicePackageItem>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServicePromotion>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServiceProduct>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<EmployeeAvailability>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<EmployeeException>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<EmployeeService>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<CustomerPaymentMethod>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<CustomerNote>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<CustomerAllergy>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<CustomerConsent>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<ServicePhoto>().HasQueryFilter(e => e.IsActive);

        // Entidades SIN filtro: registros inmutables de auditoría/log
        // AppointmentServiceItem, ProductSaleItem, InventoryMovement,
        // ReminderLog, ConfirmationToken
    }
}
