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

        // Resto de entidades multi-tenant (RA-869f17vet), con el mismo patrón.
        // Antes su aislamiento dependía de que cada consulta filtrara a mano, y
        // un olvido no fallaba: devolvía datos de otra organización en silencio.
        // TenantResolutionAndIsolation (tests) fija que TODA entidad mapeada con
        // OrganizationId tenga filtro, para que un módulo nuevo no nazca sin él.
        modelBuilder.Entity<Employee>().HasQueryFilter(
            e => CurrentOrganizationId == null || e.OrganizationId == CurrentOrganizationId);

        // AspNetUsers: las búsquedas de Identity (FindByEmailAsync, FindByIdAsync,
        // FindByLoginAsync…) quedan acotadas a la organización de la petición.
        // No rompe el login: TenantMiddleware resuelve el tenant ANTES en todas
        // las rutas /api, auth incluida. Excepción deliberada: la unicidad de
        // email y usuario es GLOBAL (índices únicos sobre toda la tabla) y la
        // comprueba GlobalUniqueUserValidator saltándose este filtro.
        //
        // Las tablas de Identity dependientes de AspNetUsers (logins, claims,
        // tokens) no llevan filtro: no tienen OrganizationId ni navegación al
        // usuario, y el store siempre las consulta por UserId de un usuario ya
        // resuelto a través de este filtro, así que no abren un camino a otra
        // organización.
        modelBuilder.Entity<User>().HasQueryFilter(
            u => CurrentOrganizationId == null || u.OrganizationId == CurrentOrganizationId);

        // RefreshToken no tiene OrganizationId propio: pertenece a la
        // organización de su usuario. Sin este filtro, un refresh token de la
        // organización A se canjeaba en el contexto de la B.
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(
            rt => CurrentOrganizationId == null || rt.User.OrganizationId == CurrentOrganizationId);
    }
}