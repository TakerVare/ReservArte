using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using AutoMapper;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Mapping;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Persistence.Repositories;
using ReservArte.Infrastructure.Services;
using ReservArte.Shared.Api;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Atomicidad de ficha + cuenta de Identity (RA-869f1811u) contra SQLite REAL,
/// con el <c>UserManager</c> y el store de EF de verdad sobre un único
/// <see cref="AppDbContext"/>, como en una petición.
///
/// Esto es lo que un doble no puede probar: que los <c>SaveChanges</c>
/// internos de Identity participan en la transacción del contexto compartido y
/// se deshacen con ella, y que tras deshacer no queda nada marcado en el change
/// tracker que un guardado posterior pudiera volver a escribir.
/// </summary>
public class EmployeeAtomicityTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public EmployeeAtomicityTests()
    {
        // Conexión abierta durante todo el test: al cerrarla SQLite descarta la
        // base en memoria.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
        context.Organizations.Add(
            new Organization { Id = OrgA, Name = "More Than Brows", Subdomain = "morethanbrows" });
        context.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private sealed class Tenant : ICurrentOrganizationService
    {
        public Guid? OrganizationId => OrgA;

        public bool IsResolved => true;

        public void SetOrganization(Guid organizationId)
        {
        }
    }

    private sealed class AdminCaller : ICurrentUserService
    {
        public int? UserId => 999;

        public string? Role => Roles.Admin;
    }

    /// <summary>
    /// Repositorio real salvo en el guardado, que falla: simula que la ficha no
    /// se puede escribir DESPUÉS de que Identity ya haya guardado la cuenta.
    /// </summary>
    private sealed class FailingSaveRepository : IEmployeeRepository
    {
        private readonly IEmployeeRepository _inner;

        public FailingSaveRepository(IEmployeeRepository inner) => _inner = inner;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("Fallo simulado al guardar la ficha.");

        public Task<PagedResult<Employee>> GetPagedAsync(
            EmployeeFilter filter, CancellationToken cancellationToken = default) =>
            _inner.GetPagedAsync(filter, cancellationToken);

        public Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            _inner.GetByIdAsync(id, cancellationToken);

        public Task<Employee?> GetByIdWithAvailabilityAsync(
            int id, CancellationToken cancellationToken = default) =>
            _inner.GetByIdWithAvailabilityAsync(id, cancellationToken);

        public Task<bool> EmailExistsAsync(
            string email, int? excludeEmployeeId = null, CancellationToken cancellationToken = default) =>
            _inner.EmailExistsAsync(email, excludeEmployeeId, cancellationToken);

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            _inner.ExistsAsync(id, cancellationToken);

        public Task<IReadOnlyList<EmployeeAvailability>> GetAvailabilitiesAsync(
            int employeeId, CancellationToken cancellationToken = default) =>
            _inner.GetAvailabilitiesAsync(employeeId, cancellationToken);

        public Task<IReadOnlyList<EmployeeException>> GetExceptionsAsync(
            int employeeId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
            _inner.GetExceptionsAsync(employeeId, from, to, cancellationToken);

        public Task<EmployeeException?> GetExceptionAsync(
            int employeeId, int exceptionId, CancellationToken cancellationToken = default) =>
            _inner.GetExceptionAsync(employeeId, exceptionId, cancellationToken);

        public void AddException(EmployeeException exception) => _inner.AddException(exception);

        public void UpdateException(EmployeeException exception) => _inner.UpdateException(exception);

        public void Add(Employee employee) => _inner.Add(employee);

        public void Update(Employee employee) => _inner.Update(employee);

        public Task ReplaceAvailabilitiesAsync(
            int employeeId,
            IEnumerable<EmployeeAvailability> availabilities,
            CancellationToken cancellationToken = default) =>
            _inner.ReplaceAvailabilitiesAsync(employeeId, availabilities, cancellationToken);
    }

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

    /// <summary>
    /// Pila real de una petición: un único AppDbContext compartido por el
    /// repositorio, la unidad de trabajo y el UserManager.
    /// </summary>
    private (EmployeeService Service, AppDbContext Context, UserManager<User> Users) CreateStack(
        Func<IEmployeeRepository, IEmployeeRepository>? decorate = null)
    {
        var tenant = new Tenant();
        var context = new AppDbContext(_options, tenant);
        var userManager = CreateUserManager(context);

        IEmployeeRepository repository = new EmployeeRepository(context, tenant);
        if (decorate is not null)
        {
            repository = decorate(repository);
        }

        var service = new EmployeeService(
            repository,
            new EfUnitOfWork(context),
            userManager,
            tenant,
            new AdminCaller(),
            Mock.Of<IEmailService>(),
            Options.Create(new AppOptions { FrontendBaseUrl = "http://localhost:3000" }),
            new MapperConfiguration(
                cfg => cfg.AddProfile<EmployeeProfile>(), NullLoggerFactory.Instance).CreateMapper(),
            NullLogger<EmployeeService>.Instance);

        return (service, context, userManager);
    }

    /// <summary>
    /// Deja una cuenta que NO es empleado (el admin, sin ficha) y una empleada
    /// dada de alta por el servicio. Devuelve el id de la empleada.
    /// </summary>
    private async Task<int> SeedAdminAccountAndEmployeeAsync()
    {
        var (service, context, users) = CreateStack();

        using (context)
        {
            var admin = await users.CreateAsync(new User
            {
                OrganizationId = OrgA,
                FirstName = "Guillermo",
                LastName = "Admin",
                Email = "guille@svalero.com",
                UserName = "guille@svalero.com",
                Rol = Roles.Admin,
            });
            admin.Succeeded.Should().BeTrue();

            var alta = await service.CreateAsync(new CreateEmployeeRequest
            {
                FirstName = "María",
                LastName = "García",
                Email = "maria@reservarte.com",
            });

            // La invitación falla aquí (no hay proveedor de tokens registrado),
            // pero sale después del commit y no revierte el alta.
            alta.Success.Should().BeTrue();

            return alta.Data!.Id;
        }
    }

    [Fact]
    public async Task Un_alta_correcta_confirma_la_ficha_y_la_cuenta_juntas()
    {
        var mariaId = await SeedAdminAccountAndEmployeeAsync();

        using var check = new AppDbContext(_options);

        (await check.Employees.AnyAsync(e => e.Id == mariaId)).Should().BeTrue();
        (await check.Users.AnyAsync(u => u.Id == mariaId)).Should().BeTrue();
    }

    [Fact]
    public async Task Si_la_ficha_no_se_guarda_la_cuenta_de_Identity_tampoco_queda()
    {
        var (service, context, _) = CreateStack(repository => new FailingSaveRepository(repository));

        using (context)
        {
            var act = () => service.CreateAsync(new CreateEmployeeRequest
            {
                FirstName = "Ana",
                LastName = "Ruiz",
                Email = "ana@reservarte.com",
            });

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        using var check = new AppDbContext(_options);

        // El UserManager SÍ guardó la cuenta (su CreateAsync hace SaveChanges),
        // pero dentro de la transacción: se deshace con la ficha.
        (await check.Users.AnyAsync(u => u.Email == "ana@reservarte.com")).Should().BeFalse(
            "la cuenta creada por Identity debe deshacerse con la ficha");
        (await check.Employees.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Editar_con_el_email_de_una_cuenta_que_no_es_empleado_no_deja_nada_a_medias()
    {
        // Reproduce el fallo visto en runtime (PUT /employees/2 con el email del
        // admin): EmailExistsAsync no lo ve porque el admin no tiene ficha.
        // Identity sí lo rechaza, y antes el guardado de la ficha persistía el
        // cambio rechazado.
        var mariaId = await SeedAdminAccountAndEmployeeAsync();

        var (service, context, _) = CreateStack();
        Result<EmployeeDto> result;

        using (context)
        {
            result = await service.UpdateAsync(mariaId, new UpdateEmployeeRequest
            {
                FirstName = "María",
                LastName = "García",
                Email = "guille@svalero.com",
                Rol = Roles.Employee,
            });
        }

        result.ErrorCode.Should().Be(ErrorCodes.GenConflict);

        using var check = new AppDbContext(_options);
        var ficha = await check.Employees.SingleAsync(e => e.Id == mariaId);
        var cuenta = await check.Users.SingleAsync(u => u.Id == mariaId);

        ficha.Email.Should().Be("maria@reservarte.com");
        cuenta.Email.Should().Be("maria@reservarte.com");
        cuenta.UserName.Should().Be("maria@reservarte.com");
        cuenta.NormalizedEmail.Should().Be("MARIA@RESERVARTE.COM");

        (await check.Users.CountAsync(u => u.Email == "guille@svalero.com")).Should().Be(1,
            "el email del admin no puede acabar repetido en otra cuenta");
    }

    [Fact]
    public async Task Tras_un_rechazo_un_guardado_posterior_no_escribe_lo_rechazado()
    {
        // Deshacer en BD no basta: el cambio rechazado queda en el change
        // tracker del contexto compartido. EfUnitOfWork lo vacía; si no, este
        // SaveChanges en la misma «petición» lo escribiría.
        var mariaId = await SeedAdminAccountAndEmployeeAsync();

        var (service, context, _) = CreateStack();

        using (context)
        {
            await service.UpdateAsync(mariaId, new UpdateEmployeeRequest
            {
                FirstName = "María",
                LastName = "García",
                Email = "guille@svalero.com",
                Rol = Roles.Employee,
            });

            await context.SaveChangesAsync();
        }

        using var check = new AppDbContext(_options);
        (await check.Users.SingleAsync(u => u.Id == mariaId)).Email.Should().Be("maria@reservarte.com");
        (await check.Employees.SingleAsync(e => e.Id == mariaId)).Email.Should().Be("maria@reservarte.com");
    }

    [Fact]
    public async Task La_baja_confirma_la_ficha_y_el_bloqueo_de_la_cuenta_juntos()
    {
        var mariaId = await SeedAdminAccountAndEmployeeAsync();

        var (service, context, _) = CreateStack();

        using (context)
        {
            (await service.DeactivateAsync(mariaId)).Success.Should().BeTrue();
        }

        using var check = new AppDbContext(_options);
        (await check.Employees.SingleAsync(e => e.Id == mariaId)).IsActive.Should().BeFalse();

        var cuenta = await check.Users.SingleAsync(u => u.Id == mariaId);
        cuenta.LockoutEnabled.Should().BeTrue();
        cuenta.LockoutEnd.Should().NotBeNull("sin bloqueo, el empleado de baja seguiría entrando");
    }

    [Fact]
    public async Task Alta_y_edicion_con_un_email_usado_en_otra_organizacion_funcionan()
    {
        // Email único por organización (RA-869f1xc0u): la misma persona puede
        // trabajar en dos centros. Otro centro ya tiene cuenta y ficha con estos
        // emails; antes, el alta y la edición devolvían GEN_CONFLICT.
        var otroCentro = new Guid("11111111-2222-3333-4444-555555555555");

        using (var seed = new AppDbContext(_options))
        {
            seed.Organizations.Add(
                new Organization { Id = otroCentro, Name = "Otro Centro", Subdomain = "otrocentro" });

            foreach (var (id, email) in new[] { (100, "lucia@correo.com"), (101, "diana@correo.com") })
            {
                seed.Users.Add(new User
                {
                    Id = id,
                    OrganizationId = otroCentro,
                    FirstName = "Otra",
                    LastName = "Empleada",
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Rol = Roles.Employee,
                    SecurityStamp = Guid.NewGuid().ToString(),
                });
                seed.Employees.Add(new Employee
                {
                    Id = id, OrganizationId = otroCentro, FirstName = "Otra", LastName = "Empleada", Email = email,
                });
            }

            await seed.SaveChangesAsync();
        }

        var mariaId = await SeedAdminAccountAndEmployeeAsync();

        Result<EmployeeDto> alta;
        var (altaService, altaContext, _) = CreateStack();
        using (altaContext)
        {
            alta = await altaService.CreateAsync(new CreateEmployeeRequest
            {
                FirstName = "Lucía",
                LastName = "Martínez",
                Email = "lucia@correo.com",
            });
        }

        Result<EmployeeDto> edicion;
        var (edicionService, edicionContext, _) = CreateStack();
        using (edicionContext)
        {
            edicion = await edicionService.UpdateAsync(mariaId, new UpdateEmployeeRequest
            {
                FirstName = "María",
                LastName = "García",
                Email = "diana@correo.com",
                Rol = Roles.Employee,
            });
        }

        alta.Success.Should().BeTrue();
        edicion.Success.Should().BeTrue();

        using var check = new AppDbContext(_options);
        (await check.Users.CountAsync(u => u.Email == "lucia@correo.com")).Should().Be(2);
        (await check.Employees.CountAsync(e => e.Email == "diana@correo.com")).Should().Be(2);
        (await check.Users.SingleAsync(u => u.Id == mariaId)).NormalizedEmail.Should().Be("DIANA@CORREO.COM");
    }
}
