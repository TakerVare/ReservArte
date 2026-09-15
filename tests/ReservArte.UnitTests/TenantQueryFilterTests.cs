using FluentAssertions;
using Microsoft.AspNetCore.Identity;
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
/// búsquedas de Identity, que es justo lo que un doble no ve. Incluye la
/// unicidad de email y de login social por organización (RA-869f1xc0u), que
/// descansa en esos mismos filtros.
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
        context.UserLogins.Add(new UserLogin
        {
            OrganizationId = OrgB,
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

    /// <summary>
    /// UserManager como el de la API: store propio y solo el validador por
    /// defecto de Identity (ya no hay validador de unicidad global).
    /// </summary>
    private static UserManager<User> CreateUserManager(AppDbContext context)
    {
        var options = new IdentityOptions();
        options.User.RequireUniqueEmail = true;

        return new UserManager<User>(
            new OrganizationUserStore(context),
            Options.Create(options),
            new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() },
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<User>>.Instance);
    }

    private static User NewAccount(Guid organizationId, string email) => new()
    {
        OrganizationId = organizationId,
        FirstName = "Otra",
        LastName = "Cuenta",
        Email = email,
        UserName = email,
        Rol = Roles.Customer,
    };

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

    [Fact]
    public void Los_indices_unicos_de_email_y_login_social_incluyen_la_organizacion()
    {
        using var context = new AppDbContext(_options);

        var users = context.Model.FindEntityType(typeof(User))!;
        var employees = context.Model.FindEntityType(typeof(Employee))!;
        var logins = context.Model.FindEntityType(typeof(UserLogin))!;

        // Ningún índice único de estas tablas puede quedar sin OrganizationId:
        // uno así volvería a imponer la unicidad global.
        users.GetIndexes().Concat(employees.GetIndexes())
            .Where(i => i.IsUnique)
            .Select(i => i.Properties[0].Name)
            .Should().NotBeEmpty().And.OnlyContain(columna => columna == "OrganizationId");

        users.GetIndexes().Where(i => i.IsUnique).Select(i => i.GetDatabaseName())
            .Should().BeEquivalentTo("EmailIndex", "UserNameIndex");

        logins.FindPrimaryKey()!.Properties.Select(p => p.Name)
            .Should().Equal("OrganizationId", "LoginProvider", "ProviderKey");
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
        (await context.UserLogins.CountAsync()).Should().Be(1);
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
        // de los proveedores.
        using var contextA = ContextFor(OrgA);
        using var contextB = ContextFor(OrgB);

        (await CreateUserManager(contextA).FindByLoginAsync("Google", "google-diana")).Should().BeNull();
        (await CreateUserManager(contextB).FindByLoginAsync("Google", "google-diana")).Should().NotBeNull();
    }

    // ── Unicidad de email por organización (RA-869f1xc0u) ────────────────

    [Fact]
    public async Task Un_email_usado_en_otra_organizacion_se_puede_usar_en_esta()
    {
        using (var context = ContextFor(OrgA))
        {
            var result = await CreateUserManager(context).CreateAsync(NewAccount(OrgA, "diana@orgb.com"));

            result.Succeeded.Should().BeTrue(
                "la misma persona puede tener cuenta en varios centros");
        }

        using var check = ContextFor(organizationId: null);
        (await check.Users.Where(u => u.Email == "diana@orgb.com").Select(u => u.OrganizationId).ToListAsync())
            .Should().BeEquivalentTo(new[] { OrgA, OrgB });
    }

    [Fact]
    public async Task Un_email_repetido_en_la_misma_organizacion_sigue_siendo_duplicado()
    {
        using var context = ContextFor(OrgA);

        var result = await CreateUserManager(context).CreateAsync(NewAccount(OrgA, "ana@orga.com"));

        result.Errors.Select(e => e.Code).Should().Contain("DuplicateEmail");
    }

    [Fact]
    public async Task La_base_de_datos_impone_la_unicidad_dentro_de_la_organizacion_y_no_fuera()
    {
        // Sin Identity de por medio: los índices (OrganizationId, NormalizedEmail)
        // y (OrganizationId, NormalizedUserName) respaldan la regla aunque un
        // camino se salte el validador.
        using (var otroCentro = ContextFor(organizationId: null))
        {
            otroCentro.Users.Add(NewUser(3, OrgB, "ana@orga.com"));
            await otroCentro.SaveChangesAsync();
        }

        using var mismoCentro = ContextFor(organizationId: null);
        mismoCentro.Users.Add(NewUser(4, OrgA, "ana@orga.com"));

        var act = () => mismoCentro.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Tampoco_las_fichas_de_empleado_chocan_entre_organizaciones()
    {
        using (var context = ContextFor(organizationId: null))
        {
            context.Users.Add(NewUser(3, OrgA, "diana@orgb.com"));
            context.Employees.Add(new Employee
            {
                Id = 3, OrganizationId = OrgA, FirstName = "Diana", LastName = "A", Email = "diana@orgb.com",
            });
            await context.SaveChangesAsync();
        }

        using var duplicada = ContextFor(organizationId: null);
        duplicada.Users.Add(NewUser(4, OrgA, "otra@orga.com"));
        duplicada.Employees.Add(new Employee
        {
            Id = 4, OrganizationId = OrgA, FirstName = "Otra", LastName = "A", Email = "diana@orgb.com",
        });

        var act = () => duplicada.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>("dentro de la organización el email sigue siendo único");
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

    [Fact]
    public async Task Sin_organizacion_resuelta_un_email_repetido_en_dos_centros_es_ambiguo()
    {
        // Auditoría de los caminos sin tenant (RA-869f1xc0u): seeders o jobs que
        // usen Identity sin fijar la organización ven todas las cuentas. Con el
        // mismo email en dos centros, la búsqueda falla. Este test lo deja a la
        // vista para que un camino así fije antes el tenant.
        using (var seed = ContextFor(organizationId: null))
        {
            seed.Users.Add(NewUser(3, OrgB, "ana@orga.com"));
            await seed.SaveChangesAsync();
        }

        using var sinTenant = ContextFor(organizationId: null);
        var act = () => CreateUserManager(sinTenant).FindByEmailAsync("ana@orga.com");

        await act.Should().ThrowAsync<InvalidOperationException>();

        using var conTenant = ContextFor(OrgA);
        (await CreateUserManager(conTenant).FindByEmailAsync("ana@orga.com"))!.Id.Should().Be(1);
    }

    // ── Login social por organización (RA-869f1xc0u) ──────────────────────

    [Fact]
    public async Task El_mismo_sujeto_del_proveedor_se_vincula_en_dos_organizaciones()
    {
        // Diana (OrgB) ya tiene vinculado google-diana. La misma cuenta de Google
        // se vincula ahora a una cuenta de OrgA: antes chocaba con la clave
        // (LoginProvider, ProviderKey) de AspNetUserLogins.
        using (var context = ContextFor(OrgA))
        {
            var users = CreateUserManager(context);
            var ana = await users.FindByIdAsync("1");

            var result = await users.AddLoginAsync(ana!, new UserLoginInfo("Google", "google-diana", "Google"));

            result.Succeeded.Should().BeTrue();
        }

        using var contextA = ContextFor(OrgA);
        using var contextB = ContextFor(OrgB);

        (await CreateUserManager(contextA).FindByLoginAsync("Google", "google-diana"))!.Id.Should().Be(1);
        (await CreateUserManager(contextB).FindByLoginAsync("Google", "google-diana"))!.Id.Should().Be(2);

        // El vínculo hereda la organización de la cuenta (OrganizationUserStore).
        using var check = ContextFor(organizationId: null);
        (await check.UserLogins.SingleAsync(l => l.UserId == 1)).OrganizationId.Should().Be(OrgA);
    }
}
