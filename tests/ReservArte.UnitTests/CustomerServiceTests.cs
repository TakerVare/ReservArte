using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Customers;
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
/// Casos de uso del módulo de Clientes (RA-869d7f369) contra SQLite real, con el
/// repositorio, el UserManager, el store y la unidad de trabajo de la API sobre
/// un único AppDbContext, como en una petición. Las reglas de este servicio son
/// justo las que un doble no puede probar: qué pasa con la cuenta de Identity
/// cuando ya existe, cuando es de personal y cuando la transacción se deshace.
/// </summary>
public class CustomerServiceTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private const string Email = "lucia@correo.com";
    private const string EmployeeEmail = "maria@reservarte.com";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly Mock<IEmailService> _emailService = new();

    public CustomerServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "More Than Brows", Subdomain = "morethanbrows" },
            new Organization { Id = OrgB, Name = "Otro Centro", Subdomain = "otrocentro" });
        context.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private sealed class Tenant : ICurrentOrganizationService
    {
        public Tenant(Guid? organizationId) => OrganizationId = organizationId;

        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;
    }

    /// <summary>Proveedor de tokens determinista, para que la invitación se pueda emitir.</summary>
    private sealed class FakeTokenProvider : IUserTwoFactorTokenProvider<User>
    {
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> manager, User user) =>
            Task.FromResult(false);

        public Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user) =>
            Task.FromResult($"{purpose}:{user.Id}:{user.SecurityStamp}");

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user) =>
            Task.FromResult(token == $"{purpose}:{user.Id}:{user.SecurityStamp}");
    }

    /// <summary>
    /// Repositorio real salvo en el guardado, que falla: la ficha no se puede
    /// escribir DESPUÉS de que Identity haya guardado la cuenta.
    /// </summary>
    private sealed class FailingSaveRepository : ICustomerRepository
    {
        private readonly ICustomerRepository _inner;

        public FailingSaveRepository(ICustomerRepository inner) => _inner = inner;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("Fallo simulado al guardar la ficha.");

        public Task<PagedResult<Customer>> GetPagedAsync(
            CustomerFilter filter, CancellationToken cancellationToken = default) =>
            _inner.GetPagedAsync(filter, cancellationToken);

        public Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            _inner.GetByIdAsync(id, cancellationToken);

        public Task<Customer?> GetProfileAsync(int id, CancellationToken cancellationToken = default) =>
            _inner.GetProfileAsync(id, cancellationToken);

        public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            _inner.GetByEmailAsync(email, cancellationToken);

        public void Add(Customer customer) => _inner.Add(customer);

        public void Update(Customer customer) => _inner.Update(customer);

        public Task<CustomerNote?> GetNoteAsync(
            int customerId, int noteId, CancellationToken cancellationToken = default) =>
            _inner.GetNoteAsync(customerId, noteId, cancellationToken);

        public void AddNote(CustomerNote note) => _inner.AddNote(note);

        public void UpdateNote(CustomerNote note) => _inner.UpdateNote(note);
    }

    private sealed class Caller : ICurrentUserService
    {
        public int? UserId { get; init; }

        public string? Role { get; init; }
    }

    private sealed record Stack(CustomerService Service, UserManager<User> Users, AppDbContext Context) : IDisposable
    {
        public void Dispose() => Context.Dispose();
    }

    private Stack CreateStack(Func<ICustomerRepository, ICustomerRepository>? decorate = null) =>
        CreateStackFor(OrgA, decorate);

    /// <summary>Pila de una petición hecha por la cuenta y el rol indicados.</summary>
    private Stack CreateStackAs(int callerId, string callerRole) =>
        CreateStackFor(OrgA, caller: new Caller { UserId = callerId, Role = callerRole });

    /// <summary>Pila de una petición en el centro indicado (null = sin organización resuelta).</summary>
    private Stack CreateStackFor(
        Guid? organizationId,
        Func<ICustomerRepository, ICustomerRepository>? decorate = null,
        Caller? caller = null)
    {
        var tenant = new Tenant(organizationId);
        var context = new AppDbContext(_options, tenant);

        var identityOptions = new IdentityOptions();
        identityOptions.User.RequireUniqueEmail = true;

        var users = new UserManager<User>(
            new OrganizationUserStore(context),
            Options.Create(identityOptions),
            new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() },
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<User>>.Instance);
        users.RegisterTokenProvider(InvitationTokenDefaults.ProviderName, new FakeTokenProvider());

        ICustomerRepository repository = new CustomerRepository(context, tenant);
        if (decorate is not null)
        {
            repository = decorate(repository);
        }

        var service = new CustomerService(
            repository,
            new EmployeeRepository(context, tenant),
            new EfUnitOfWork(context),
            users,
            tenant,
            caller ?? new Caller(),
            _emailService.Object,
            Options.Create(new AppOptions { FrontendBaseUrl = "http://localhost:3000" }),
            new MapperConfiguration(
                cfg => cfg.AddProfile<CustomerProfile>(), NullLoggerFactory.Instance).CreateMapper(),
            NullLogger<CustomerService>.Instance);

        return new Stack(service, users, context);
    }

    private static CreateCustomerRequest NewRequest(
        string email = Email,
        string firstName = "Lucía",
        string lastName = "Martínez",
        string? category = null,
        params string[] consents) => new()
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = "+34600111222",
            Category = category,
            GrantedConsents = consents.Length == 0 ? new[] { CustomerConsentTypes.DataProcessing } : consents,
        };

    private static UpdateCustomerRequest EditRequest(
        string email, string firstName = "Lucía", string lastName = "Martínez", string phone = "+34600111222") => new()
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            Category = CustomerCategories.Vip,
            PreferredContactMethod = CustomerContactMethods.WhatsApp,
        };

    private async Task<Result<CustomerDetailDto>> CreateAsync(CreateCustomerRequest request)
    {
        using var stack = CreateStack();
        return await stack.Service.CreateAsync(request);
    }

    private async Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        using var stack = CreateStack();
        return await stack.Service.UpdateAsync(id, request);
    }

    /// <summary>Cuenta del centro sin ficha de cliente (una empleada, el admin…).</summary>
    private async Task<int> SeedAccountAsync(
        string email, string rol, string firstName = "María", string lastName = "García")
    {
        using var stack = CreateStack();
        var user = new User
        {
            OrganizationId = OrgA,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            UserName = email,
            PhoneNumber = "+34600999999",
            Rol = rol,
        };

        (await stack.Users.CreateAsync(user)).Succeeded.Should().BeTrue();

        return user.Id;
    }

    /// <summary>Cuenta y ficha en otro centro con el email indicado.</summary>
    private async Task<int> SeedCustomerInOrgBAsync(string email)
    {
        using var context = new AppDbContext(_options);
        var user = new User
        {
            Id = 500,
            OrganizationId = OrgB,
            FirstName = "Otra",
            LastName = "Clienta",
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Rol = Roles.Customer,
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        context.Users.Add(user);
        context.Customers.Add(new Customer
        {
            Id = user.Id,
            OrganizationId = OrgB,
            FirstName = "Otra",
            LastName = "Clienta",
            Email = email,
        });
        await context.SaveChangesAsync();

        return user.Id;
    }

    private void VerifyNoInvitationSent() =>
        _emailService.Verify(
            e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);

    // ── Alta ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task El_alta_de_una_clienta_nueva_crea_cuenta_sin_contrasena_ficha_new_y_solo_lo_marcado()
    {
        var result = await CreateAsync(NewRequest(
            consents: new[] { CustomerConsentTypes.DataProcessing, CustomerConsentTypes.Marketing }));

        result.Success.Should().BeTrue(result.ErrorMessage);
        result.Data!.Category.Should().Be(CustomerCategories.New);
        result.Data.Consents.Select(c => c.ConsentType).Should().BeEquivalentTo(
            new[] { CustomerConsentTypes.DataProcessing, CustomerConsentTypes.Marketing });

        using var check = new AppDbContext(_options);

        var cuenta = await check.Users.SingleAsync();
        cuenta.Rol.Should().Be(Roles.Customer);
        cuenta.PasswordHash.Should().BeNull("la contraseña la crea la clienta desde la invitación");
        cuenta.NormalizedEmail.Should().Be("LUCIA@CORREO.COM");

        var ficha = await check.Customers.SingleAsync();
        ficha.Id.Should().Be(cuenta.Id, "la ficha comparte el Id de la cuenta");
        ficha.OrganizationId.Should().Be(OrgA);
        ficha.Category.Should().Be(CustomerCategories.New);
        ficha.PreferredContactMethod.Should().Be(CustomerContactMethods.Email);

        var consentimientos = await check.CustomerConsents.ToListAsync();
        consentimientos.Should().HaveCount(2).And.AllSatisfy(c =>
        {
            c.CustomerId.Should().Be(cuenta.Id);
            c.OrganizationId.Should().Be(OrgA);
            c.IsGranted.Should().BeTrue();
            c.GrantedAt.Should().NotBeNull();
        });

        _emailService.Verify(
            e => e.SendAsync(
                It.Is<EmailMessage>(m => m.To == Email && m.Body.Contains("/set-password/")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Sin_tratamiento_de_datos_no_hay_alta_ni_invitacion()
    {
        var result = await CreateAsync(NewRequest(consents: new[] { CustomerConsentTypes.Marketing }));

        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);

        using var check = new AppDbContext(_options);
        (await check.Users.CountAsync()).Should().Be(0);
        (await check.Customers.CountAsync()).Should().Be(0);
        (await check.CustomerConsents.CountAsync()).Should().Be(0);
        VerifyNoInvitationSent();
    }

    [Fact]
    public async Task La_categoria_indicada_por_el_centro_se_respeta()
    {
        var result = await CreateAsync(NewRequest(category: CustomerCategories.Vip));

        result.Success.Should().BeTrue(result.ErrorMessage);

        using var check = new AppDbContext(_options);
        (await check.Customers.SingleAsync()).Category.Should().Be(CustomerCategories.Vip);
    }

    [Fact]
    public async Task Un_email_con_ficha_en_el_centro_es_conflicto_y_no_se_invita_de_nuevo()
    {
        (await CreateAsync(NewRequest())).Success.Should().BeTrue();

        var repetido = await CreateAsync(NewRequest(firstName: "Otra"));

        repetido.ErrorCode.Should().Be(ErrorCodes.GenConflict);

        using var check = new AppDbContext(_options);
        (await check.Customers.CountAsync()).Should().Be(1);
        (await check.Users.CountAsync()).Should().Be(1);
        _emailService.Verify(
            e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dar_de_alta_como_clienta_a_una_empleada_anade_la_ficha_a_su_cuenta_sin_tocarla()
    {
        var mariaId = await SeedAccountAsync(EmployeeEmail, Roles.Employee);

        var result = await CreateAsync(NewRequest(
            email: EmployeeEmail, firstName: "María", lastName: "García López"));

        result.Success.Should().BeTrue(result.ErrorMessage);
        result.Data!.Id.Should().Be(mariaId, "la ficha se añade a la cuenta existente");

        using var check = new AppDbContext(_options);
        (await check.Users.CountAsync()).Should().Be(1, "no se crea otra cuenta");

        var cuenta = await check.Users.SingleAsync();
        cuenta.Rol.Should().Be(Roles.Employee, "la ficha de cliente no cambia el rol de personal");
        cuenta.LastName.Should().Be("García", "la cuenta existente no se modifica");

        var ficha = await check.Customers.SingleAsync();
        ficha.Id.Should().Be(mariaId);
        ficha.LastName.Should().Be("García López");
        (await check.CustomerConsents.SingleAsync()).CustomerId.Should().Be(mariaId);

        // Ya sabe entrar: invitarla sería mandarle un cambio de contraseña.
        VerifyNoInvitationSent();
    }

    [Fact]
    public async Task Una_cuenta_que_ya_tiene_ficha_de_cliente_no_recibe_otra()
    {
        // Ficha con un email distinto al de la cuenta: la comprobación por email
        // de la ficha no la ve, la de la cuenta sí.
        var sofiaId = await SeedAccountAsync("sofia@correo.com", Roles.Customer, "Sofía", "Ruiz");
        using (var seed = new AppDbContext(_options))
        {
            seed.Customers.Add(new Customer
            {
                Id = sofiaId,
                OrganizationId = OrgA,
                FirstName = "Sofía",
                LastName = "Ruiz",
                Email = "sofia.antigua@correo.com",
            });
            await seed.SaveChangesAsync();
        }

        var result = await CreateAsync(NewRequest(email: "sofia@correo.com"));

        result.ErrorCode.Should().Be(ErrorCodes.GenConflict);

        using var check = new AppDbContext(_options);
        (await check.Customers.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task El_mismo_email_en_otro_centro_no_es_conflicto()
    {
        await SeedCustomerInOrgBAsync(Email);

        var result = await CreateAsync(NewRequest());

        result.Success.Should().BeTrue(result.ErrorMessage);

        using var check = new AppDbContext(_options);
        (await check.Users.CountAsync(u => u.Email == Email)).Should().Be(2);
        (await check.Customers.SingleAsync(c => c.Id == result.Data!.Id)).OrganizationId.Should().Be(OrgA);
    }

    [Fact]
    public async Task Si_la_ficha_no_se_guarda_la_cuenta_tampoco_queda_ni_se_invita()
    {
        using (var stack = CreateStack(repository => new FailingSaveRepository(repository)))
        {
            var act = () => stack.Service.CreateAsync(NewRequest());

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        // Identity SÍ guardó la cuenta (su CreateAsync hace SaveChanges), pero
        // dentro de la transacción: se deshace con la ficha.
        using var check = new AppDbContext(_options);
        (await check.Users.AnyAsync(u => u.Email == Email)).Should().BeFalse();
        (await check.CustomerConsents.CountAsync()).Should().Be(0);
        VerifyNoInvitationSent();
    }

    [Fact]
    public async Task Sin_organizacion_resuelta_no_hay_alta()
    {
        using var stack = CreateStackFor(organizationId: null);

        var result = await stack.Service.CreateAsync(NewRequest());

        result.ErrorCode.Should().Be(ErrorCodes.OrgTenantNotResolved);
    }

    // ── Edición ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Editar_la_ficha_de_una_clienta_sincroniza_su_cuenta()
    {
        var id = (await CreateAsync(NewRequest())).Data!.Id;

        var result = await UpdateAsync(id, EditRequest(
            "lucia.nueva@correo.com", firstName: "Lucía", lastName: "Martín", phone: "+34611000000"));

        result.Success.Should().BeTrue(result.ErrorMessage);

        using var check = new AppDbContext(_options);

        var ficha = await check.Customers.SingleAsync();
        ficha.Email.Should().Be("lucia.nueva@correo.com");
        ficha.LastName.Should().Be("Martín");
        ficha.Category.Should().Be(CustomerCategories.Vip);
        ficha.PreferredContactMethod.Should().Be(CustomerContactMethods.WhatsApp);
        ficha.UpdatedAt.Should().NotBeNull();

        var cuenta = await check.Users.SingleAsync();
        cuenta.Email.Should().Be("lucia.nueva@correo.com");
        cuenta.UserName.Should().Be("lucia.nueva@correo.com");
        cuenta.NormalizedEmail.Should().Be("LUCIA.NUEVA@CORREO.COM", "es el que usa el login");
        cuenta.LastName.Should().Be("Martín");
        cuenta.PhoneNumber.Should().Be("+34611000000");
    }

    [Fact]
    public async Task Editar_sin_cambiar_el_email_no_lo_marca_como_no_confirmado()
    {
        var id = (await CreateAsync(NewRequest())).Data!.Id;
        using (var seed = new AppDbContext(_options))
        {
            (await seed.Users.SingleAsync()).EmailConfirmed = true;
            await seed.SaveChangesAsync();
        }

        (await UpdateAsync(id, EditRequest(Email, firstName: "Lucía María"))).Success.Should().BeTrue();

        using var check = new AppDbContext(_options);
        var cuenta = await check.Users.SingleAsync();
        cuenta.FirstName.Should().Be("Lucía María");
        cuenta.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task En_una_cuenta_de_personal_la_edicion_solo_toca_la_ficha()
    {
        var mariaId = await SeedAccountAsync(EmployeeEmail, Roles.Employee);
        (await CreateAsync(NewRequest(email: EmployeeEmail, firstName: "María", lastName: "García")))
            .Success.Should().BeTrue();

        var result = await UpdateAsync(mariaId, EditRequest(
            EmployeeEmail, firstName: "Mari", lastName: "García", phone: "+34622000000"));

        result.Success.Should().BeTrue(result.ErrorMessage);

        using var check = new AppDbContext(_options);
        var ficha = await check.Customers.SingleAsync();
        ficha.FirstName.Should().Be("Mari");
        ficha.Phone.Should().Be("+34622000000");

        var cuenta = await check.Users.SingleAsync();
        cuenta.FirstName.Should().Be("María", "la cuenta de personal se gestiona desde Empleados");
        cuenta.PhoneNumber.Should().Be("+34600999999");
        cuenta.Rol.Should().Be(Roles.Employee);
    }

    [Fact]
    public async Task En_una_cuenta_de_personal_el_email_no_se_cambia_desde_Clientes()
    {
        var mariaId = await SeedAccountAsync(EmployeeEmail, Roles.Admin);
        (await CreateAsync(NewRequest(email: EmployeeEmail, firstName: "María", lastName: "García")))
            .Success.Should().BeTrue();

        var result = await UpdateAsync(mariaId, EditRequest("otra@correo.com", firstName: "María"));

        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);

        using var check = new AppDbContext(_options);
        (await check.Customers.SingleAsync()).Email.Should().Be(EmployeeEmail);
        (await check.Users.SingleAsync()).NormalizedEmail.Should().Be("MARIA@RESERVARTE.COM");
    }

    [Fact]
    public async Task Editar_con_el_email_de_otra_ficha_es_conflicto()
    {
        var luciaId = (await CreateAsync(NewRequest())).Data!.Id;
        (await CreateAsync(NewRequest(email: "sofia@correo.com", firstName: "Sofía"))).Success.Should().BeTrue();

        var result = await UpdateAsync(luciaId, EditRequest("sofia@correo.com"));

        result.ErrorCode.Should().Be(ErrorCodes.GenConflict);

        using var check = new AppDbContext(_options);
        (await check.Customers.SingleAsync(c => c.Id == luciaId)).Email.Should().Be(Email);
    }

    [Fact]
    public async Task Editar_con_el_email_de_una_cuenta_sin_ficha_no_deja_nada_a_medias()
    {
        // La cuenta del admin no tiene ficha: la comprobación de fichas no la ve,
        // Identity sí la rechaza. El cambio rechazado queda en el contexto
        // compartido y no debe acabar guardado con la ficha.
        await SeedAccountAsync("guille@svalero.com", Roles.Admin, "Guillermo", "Admin");
        var luciaId = (await CreateAsync(NewRequest())).Data!.Id;

        var result = await UpdateAsync(luciaId, EditRequest("guille@svalero.com"));

        result.ErrorCode.Should().Be(ErrorCodes.GenConflict);

        using var check = new AppDbContext(_options);
        (await check.Customers.SingleAsync()).Email.Should().Be(Email);
        (await check.Users.SingleAsync(u => u.Id == luciaId)).NormalizedEmail.Should().Be("LUCIA@CORREO.COM");
        (await check.Users.CountAsync(u => u.Email == "guille@svalero.com")).Should().Be(1);
    }

    [Fact]
    public async Task Editar_un_cliente_de_otro_centro_es_no_encontrado()
    {
        var otroId = await SeedCustomerInOrgBAsync("otra@correo.com");

        var result = await UpdateAsync(otroId, EditRequest("otra@correo.com"));

        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    // ── Baja ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task La_baja_de_la_ficha_no_bloquea_la_cuenta_y_es_reversible()
    {
        var mariaId = await SeedAccountAsync(EmployeeEmail, Roles.Employee);
        (await CreateAsync(NewRequest(email: EmployeeEmail))).Success.Should().BeTrue();

        using (var stack = CreateStack())
        {
            (await stack.Service.DeactivateAsync(mariaId)).Data!.IsActive.Should().BeFalse();
        }

        using (var stack = CreateStack())
        {
            // Idempotente.
            (await stack.Service.DeactivateAsync(mariaId)).Success.Should().BeTrue();
        }

        using (var check = new AppDbContext(_options))
        {
            (await check.Customers.SingleAsync()).IsActive.Should().BeFalse();
            (await check.Users.SingleAsync()).LockoutEnd.Should().BeNull(
                "la baja como clienta no puede dejar sin acceso a la empleada");
        }

        using (var stack = CreateStack())
        {
            (await stack.Service.ReactivateAsync(mariaId)).Data!.IsActive.Should().BeTrue();
        }
    }

    // ── Consultas ─────────────────────────────────────────────────────────

    [Fact]
    public async Task El_perfil_devuelve_la_ficha_con_sus_consentimientos_vigentes()
    {
        var id = (await CreateAsync(NewRequest(
            consents: new[] { CustomerConsentTypes.DataProcessing, CustomerConsentTypes.Photos }))).Data!.Id;

        using var stack = CreateStack();
        var result = await stack.Service.GetByIdAsync(id);

        result.Success.Should().BeTrue(result.ErrorMessage);
        result.Data!.FullName.Should().Be("Lucía Martínez");
        result.Data.Consents.Select(c => c.ConsentType).Should().BeEquivalentTo(
            new[] { CustomerConsentTypes.DataProcessing, CustomerConsentTypes.Photos });
    }

    [Fact]
    public async Task El_perfil_de_un_cliente_de_otro_centro_es_no_encontrado()
    {
        var otroId = await SeedCustomerInOrgBAsync(Email);

        using var stack = CreateStack();
        var result = await stack.Service.GetByIdAsync(otroId);

        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    [Fact]
    public async Task La_lista_solo_devuelve_las_fichas_del_centro()
    {
        await SeedCustomerInOrgBAsync("otra@correo.com");
        (await CreateAsync(NewRequest())).Success.Should().BeTrue();
        (await CreateAsync(NewRequest(email: "sofia@correo.com", firstName: "Sofía", lastName: "Ruiz")))
            .Success.Should().BeTrue();

        using var stack = CreateStack();
        var result = await stack.Service.GetPagedAsync(new CustomerFilter());

        result.Success.Should().BeTrue(result.ErrorMessage);
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.Select(c => c.FullName).Should().BeEquivalentTo(
            new[] { "Lucía Martínez", "Sofía Ruiz" });
    }

    [Fact]
    public async Task Sin_organizacion_resuelta_la_lista_falla()
    {
        using var stack = CreateStackFor(organizationId: null);

        var result = await stack.Service.GetPagedAsync(new CustomerFilter());

        result.ErrorCode.Should().Be(ErrorCodes.OrgTenantNotResolved);
    }

    // ── Notas internas (RA-869d7f3fw) ─────────────────────────────────────

    /// <summary>Cuenta del centro con ficha de empleado. Devuelve su id.</summary>
    private async Task<int> SeedEmployeeAsync(string email, string rol = Roles.Employee, bool isActive = true)
    {
        var id = await SeedAccountAsync(email, rol);

        using var seed = new AppDbContext(_options);
        seed.Employees.Add(new Employee
        {
            Id = id,
            OrganizationId = OrgA,
            FirstName = "María",
            LastName = "García",
            Email = email,
            Rol = rol,
            IsActive = isActive,
        });
        await seed.SaveChangesAsync();

        return id;
    }

    private async Task<Result<CustomerNoteDto>> AddNoteAs(
        int callerId, string callerRole, int customerId, string note = "  Prefiere citas por la tarde.  ")
    {
        using var stack = CreateStackAs(callerId, callerRole);
        return await stack.Service.AddNoteAsync(customerId, new CreateCustomerNoteRequest { Note = note });
    }

    private async Task<Result<CustomerNoteDto>> DeleteNoteAs(
        int callerId, string callerRole, int customerId, int noteId)
    {
        using var stack = CreateStackAs(callerId, callerRole);
        return await stack.Service.DeleteNoteAsync(customerId, noteId);
    }

    [Fact]
    public async Task Una_empleada_anade_una_nota_firmada_con_su_ficha()
    {
        var mariaId = await SeedEmployeeAsync(EmployeeEmail);
        var luciaId = (await CreateAsync(NewRequest())).Data!.Id;

        var result = await AddNoteAs(mariaId, Roles.Employee, luciaId);

        result.Success.Should().BeTrue(result.ErrorMessage);
        result.Data!.EmployeeId.Should().Be(mariaId);

        using var check = new AppDbContext(_options);
        var nota = await check.CustomerNotes.SingleAsync();
        nota.CustomerId.Should().Be(luciaId);
        nota.EmployeeId.Should().Be(mariaId);
        nota.OrganizationId.Should().Be(OrgA);
        nota.Note.Should().Be("Prefiere citas por la tarde.");
        nota.IsActive.Should().BeTrue();

        using var stack = CreateStack();
        (await stack.Service.GetByIdAsync(luciaId)).Data!.Notes.Should().ContainSingle();
    }

    [Fact]
    public async Task Una_cuenta_de_personal_sin_ficha_de_empleado_no_puede_escribir_notas()
    {
        var adminId = await SeedAccountAsync("guille@svalero.com", Roles.Admin, "Guillermo", "Admin");
        var luciaId = (await CreateAsync(NewRequest())).Data!.Id;

        var result = await AddNoteAs(adminId, Roles.Admin, luciaId);

        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);

        using var check = new AppDbContext(_options);
        (await check.CustomerNotes.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Una_empleada_de_baja_no_puede_escribir_notas()
    {
        var mariaId = await SeedEmployeeAsync(EmployeeEmail, isActive: false);
        var luciaId = (await CreateAsync(NewRequest())).Data!.Id;

        (await AddNoteAs(mariaId, Roles.Employee, luciaId)).ErrorCode.Should().Be(ErrorCodes.GenForbidden);
    }

    [Fact]
    public async Task Una_nota_a_un_cliente_de_otro_centro_es_no_encontrado()
    {
        var mariaId = await SeedEmployeeAsync(EmployeeEmail);
        var otroId = await SeedCustomerInOrgBAsync("otra@correo.com");

        (await AddNoteAs(mariaId, Roles.Employee, otroId)).ErrorCode.Should().Be(ErrorCodes.GenNotFound);

        using var check = new AppDbContext(_options);
        (await check.CustomerNotes.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task La_autora_retira_su_nota_de_forma_idempotente_y_deja_de_verse_en_el_perfil()
    {
        var mariaId = await SeedEmployeeAsync(EmployeeEmail);
        var luciaId = (await CreateAsync(NewRequest())).Data!.Id;
        var noteId = (await AddNoteAs(mariaId, Roles.Employee, luciaId)).Data!.Id;

        (await DeleteNoteAs(mariaId, Roles.Employee, luciaId, noteId)).Data!.Should().NotBeNull();
        (await DeleteNoteAs(mariaId, Roles.Employee, luciaId, noteId)).Success.Should().BeTrue();

        using (var check = new AppDbContext(_options))
        {
            (await check.CustomerNotes.SingleAsync()).IsActive.Should().BeFalse("la baja es lógica");
        }

        using var stack = CreateStack();
        (await stack.Service.GetByIdAsync(luciaId)).Data!.Notes.Should().BeEmpty();
    }

    [Fact]
    public async Task Otra_empleada_no_puede_retirar_una_nota_ajena()
    {
        var mariaId = await SeedEmployeeAsync(EmployeeEmail);
        var anaId = await SeedEmployeeAsync("ana@reservarte.com");
        var luciaId = (await CreateAsync(NewRequest())).Data!.Id;
        var noteId = (await AddNoteAs(mariaId, Roles.Employee, luciaId)).Data!.Id;

        (await DeleteNoteAs(anaId, Roles.Employee, luciaId, noteId)).ErrorCode.Should().Be(ErrorCodes.GenForbidden);

        using var check = new AppDbContext(_options);
        (await check.CustomerNotes.SingleAsync()).IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(Roles.Manager)]
    [InlineData(Roles.Admin)]
    public async Task Un_Manager_o_Admin_retira_una_nota_que_no_escribio(string rol)
    {
        var mariaId = await SeedEmployeeAsync(EmployeeEmail);
        var gestoraId = await SeedAccountAsync("gestora@reservarte.com", rol, "Gestora", "Centro");
        var luciaId = (await CreateAsync(NewRequest())).Data!.Id;
        var noteId = (await AddNoteAs(mariaId, Roles.Employee, luciaId)).Data!.Id;

        (await DeleteNoteAs(gestoraId, rol, luciaId, noteId)).Success.Should().BeTrue();

        using var check = new AppDbContext(_options);
        (await check.CustomerNotes.SingleAsync()).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Retirar_una_nota_indicando_otro_cliente_es_no_encontrado()
    {
        var mariaId = await SeedEmployeeAsync(EmployeeEmail);
        var luciaId = (await CreateAsync(NewRequest())).Data!.Id;
        var sofiaId = (await CreateAsync(NewRequest(email: "sofia@correo.com", firstName: "Sofía"))).Data!.Id;
        var noteId = (await AddNoteAs(mariaId, Roles.Employee, luciaId)).Data!.Id;

        (await DeleteNoteAs(mariaId, Roles.Employee, sofiaId, noteId)).ErrorCode.Should().Be(ErrorCodes.GenNotFound);

        using var check = new AppDbContext(_options);
        (await check.CustomerNotes.SingleAsync()).IsActive.Should().BeTrue();
    }
}
