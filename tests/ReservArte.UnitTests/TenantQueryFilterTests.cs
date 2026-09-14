using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Persistence;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Query filters globales por organización en el resto de entidades
/// multi-tenant (RA-869f17vet), contra SQLite real y con el UserManager de
/// Identity de verdad: el filtro sobre AspNetUsers afecta a TODAS las
/// búsquedas de Identity, que es justo lo que un doble no ve.
/// </summary>
public class TenantQueryFilterTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public TenantQueryFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Sin tenant, como un seeder: el filtro no restringe y se siembran las
        // dos organizaciones.
        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
        Seed(context);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class Tenant : ICurrentOrganizationService
    {
        public Tenant(Guid? organizationId) => OrganizationId = organizationId;

        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;
    }

    private AppDbContext ContextFor(Guid? organizationId) =>
        new(_options, new Tenant(organizationId));

    private static void Seed(AppDbContext context)
    {
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "Org A", Subdomain = "org-a" },
            new Organization { Id = OrgB, Name = "Org B", Subdomain = "org-b" });

        context.Users.AddRange(
            NewUser(1, OrgA, "ana@orga.com"),
            NewUser(2, OrgB, "diana@orgb.com"));

        context.Employees.AddRange(
            new Employee { Id = 1, OrganizationId = OrgA, FirstName = "Ana", LastName = "A", Email = "ana@orga.com" },
            new Employee { Id = 2, OrganizationId = OrgB, FirstName = "Diana", LastName = "D", Email = "diana@orgb.com" });

        context.RefreshTokens.AddRange(
            NewRefreshToken(userId: 1, token: "token-de-ana"),
            NewRefreshToken(userId: 2, token: "token-de-diana"));

        // Vínculo de login social de Diana (OrgB), para el caso del callback OAuth.
        context.UserLogins.Add(new IdentityUserLogin<int>
        {
            LoginProvider = "Google",
            ProviderKey = "google-diana",
            ProviderDisplayName = "Google",
            UserId = 2,
        });

        context.SaveChanges();
    }

    private static User NewUser(int id, Guid organizationId, string email) => new()
    {
        Id = id,
        OrganizationId = organizationId,
        FirstName = "N",
        LastName = "A",
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Rol = Roles.Employee,
        SecurityStamp = Guid.NewGuid().ToString(),
    };

    private static RefreshToken NewRefreshToken(int userId, string token) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Token = token,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(30),
    };

    private static UserManager<User> CreateUserManager(
        AppDbContext context, bool withGlobalUniqueValidator = true)
    {
        var options = new IdentityOptions();
        options.User.RequireUniqueEmail = true;

        var validators = new List<IUserValidator<User>> { new UserValidator<User>() };
        if (withGlobalUniqueValidator)
        {
            validators.Add(new GlobalUniqueUserValidator(context));
        }

        return new UserManager<User>(
            new UserOnlyStore<User, AppDbContext, int>(context),
            Options.Create(options),
            new PasswordHasher<User>(),
            validators,
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<User>>.Instance);
    }

    // ── El modelo: ninguna entidad multi-tenant sin filtro ────────────────

    [Fact]
    public void Toda_entidad_mapeada_con_OrganizationId_tiene_query_filter()
    {
        using var context = new AppDbContext(_options);

        var sinFiltro = context.Model.GetEntityTypes()
            .Where(t => t.ClrType.GetProperty("OrganizationId") is not null)
            .Where(t => t.GetQueryFilter() is null)
            .Select(t => t.ClrType.Name)
            .ToList();

        // Es la red para los módulos que vienen (Clientes, Servicios, Citas…):
        // una entidad nueva con OrganizationId sin filtro hace fallar este test
        // en vez de filtrar datos de otra organización en silencio.
        sinFiltro.Should().BeEmpty();
    }

    [Fact]
    public void RefreshToken_tiene_filtro_aunque_no_tenga_OrganizationId_propio()
    {
        using var context = new AppDbContext(_options);

        context.Model.FindEntityType(typeof(RefreshToken))!.GetQueryFilter().Should().NotBeNull(
            "pertenece a la organización de su usuario");
    }

    // ── Aislamiento por entidad ───────────────────────────────────────────

    [Fact]
    public async Task Users_solo_devuelve_cuentas_de_la_organizacion_actual()
    {
        using var context = ContextFor(OrgA);

        var cuentas = await context.Users.ToListAsync();

        cuentas.Should().ContainSingle().Which.Email.Should().Be("ana@orga.com");
    }

    [Fact]
    public async Task Employees_solo_devuelve_fichas_de_la_organizacion_actual()
    {
        using var context = ContextFor(OrgB);

        var fichas = await context.Employees.ToListAsync();

        fichas.Should().ContainSingle().Which.Email.Should().Be("diana@orgb.com");
    }

    [Fact]
    public async Task Un_refresh_token_de_otra_organizacion_no_se_encuentra()
    {
        using var context = ContextFor(OrgA);

        // Es la consulta de AuthService.RefreshTokenAsync. Antes, el token de
        // Diana (OrgB) se encontraba y se canjeaba en el contexto de OrgA.
        var propio = await context.RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == "token-de-ana");
        var ajeno = await context.RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == "token-de-diana");

        propio.Should().NotBeNull();
        ajeno.Should().BeNull();
    }

    [Fact]
    public async Task Sin_organizacion_resuelta_el_filtro_no_restringe()
    {
        // Migraciones, seeders y dotnet ef trabajan sin petición: deben verlo todo.
        using var context = ContextFor(organizationId: null);

        (await context.Users.CountAsync()).Should().Be(2);
        (await context.Employees.CountAsync()).Should().Be(2);
        (await context.RefreshTokens.CountAsync()).Should().Be(2);
    }

    // ── Identity con el filtro activo ─────────────────────────────────────

    [Fact]
    public async Task Identity_no_encuentra_por_email_una_cuenta_de_otra_organizacion()
    {
        using var contextA = ContextFor(OrgA);
        using var contextB = ContextFor(OrgB);

        (await CreateUserManager(contextA).FindByEmailAsync("diana@orgb.com")).Should().BeNull();
        (await CreateUserManager(contextB).FindByEmailAsync("diana@orgb.com")).Should().NotBeNull();
    }

    [Fact]
    public async Task Identity_no_resuelve_por_login_externo_una_cuenta_de_otra_organizacion()
    {
        // Es la búsqueda del callback OAuth (ExternalLoginAsync → FindByLoginAsync),
        // que en desarrollo no se puede probar en runtime sin credenciales reales
        // de los proveedores. AspNetUserLogins no lleva filtro, pero el store
        // resuelve después el usuario por Id a través del filtro de AspNetUsers.
        using var contextA = ContextFor(OrgA);
        using var contextB = ContextFor(OrgB);

        (await CreateUserManager(contextA).FindByLoginAsync("Google", "google-diana")).Should().BeNull();
        (await CreateUserManager(contextB).FindByLoginAsync("Google", "google-diana")).Should().NotBeNull();
    }

    [Fact]
    public async Task Un_email_de_otra_organizacion_se_rechaza_como_duplicado_y_no_como_error_de_BD()
    {
        using var context = ContextFor(OrgA);
        var users = CreateUserManager(context);

        var result = await users.CreateAsync(new User
        {
            OrganizationId = OrgA,
            FirstName = "Otra",
            LastName = "Diana",
            Email = "diana@orgb.com",
            UserName = "diana@orgb.com",
            Rol = Roles.Customer,
        });

        result.Succeeded.Should().BeFalse();
        result.Errors.Select(e => e.Code).Should().Contain("DuplicateEmail");
    }

    [Fact]
    public async Task Sin_el_validador_global_ese_choque_llegaria_a_la_base_de_datos()
    {
        // Documenta POR QUÉ existe GlobalUniqueUserValidator: con el filtro, el
        // validador por defecto de Identity no ve la cuenta de OrgB y el alta
        // choca con el índice único global al guardar.
        using var context = ContextFor(OrgA);
        var users = CreateUserManager(context, withGlobalUniqueValidator: false);

        var act = () => users.CreateAsync(new User
        {
            OrganizationId = OrgA,
            FirstName = "Otra",
            LastName = "Diana",
            Email = "diana@orgb.com",
            UserName = "diana@orgb.com",
            Rol = Roles.Customer,
        });

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Un_email_repetido_en_la_misma_organizacion_sigue_siendo_duplicado()
    {
        using var context = ContextFor(OrgA);

        var result = await CreateUserManager(context).CreateAsync(new User
        {
            OrganizationId = OrgA,
            FirstName = "Otra",
            LastName = "Ana",
            Email = "ana@orga.com",
            UserName = "ana@orga.com",
            Rol = Roles.Customer,
        });

        result.Errors.Select(e => e.Code).Should().Contain("DuplicateEmail");
    }

    [Fact]
    public async Task Editar_la_propia_cuenta_no_choca_consigo_misma()
    {
        using var context = ContextFor(OrgA);
        var users = CreateUserManager(context);

        var ana = await users.FindByEmailAsync("ana@orga.com");
        ana!.PhoneNumber = "600000000";

        (await users.UpdateAsync(ana)).Succeeded.Should().BeTrue();
    }
}
