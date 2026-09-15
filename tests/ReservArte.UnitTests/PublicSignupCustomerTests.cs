using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ReservArte.Application.DTOs.Auth;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Validators.Auth;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Services;
using ReservArte.Shared.Api;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// El alta pública crea la ficha de cliente (RA-869f1xc2n): registro local y
/// alta social dejan cuenta y ficha juntas, en una transacción, y el registro
/// guarda el consentimiento de tratamiento de datos que la persona marca.
///
/// Contra SQLite real con el UserManager, el store y la unidad de trabajo de la
/// API sobre un único AppDbContext, como en una petición: la atomicidad entre
/// Identity y la ficha es justo lo que un doble no puede probar.
/// </summary>
public class PublicSignupCustomerTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

    private const string Email = "lucia@correo.com";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public PublicSignupCustomerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
        context.Organizations.Add(new Organization { Id = OrgA, Name = "More Than Brows", Subdomain = "morethanbrows" });
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

    private sealed record Stack(AuthService Auth, AppDbContext Context) : IDisposable
    {
        public void Dispose() => Context.Dispose();
    }

    /// <summary>Pila de una petición: AuthService, UserManager y unidad de trabajo sobre un contexto.</summary>
    private Stack CreateStack()
    {
        var context = new AppDbContext(_options, new Tenant());

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

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<Guid>())).Returns("access");
        jwt.Setup(j => j.GenerateRefreshToken()).Returns(() => Guid.NewGuid().ToString());

        var captcha = new Mock<ICaptchaService>();
        captcha.Setup(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(true);

        var auth = new AuthService(
            users,
            jwt.Object,
            context,
            Options.Create(new JwtOptions { RefreshTokenDays = 30 }),
            captcha.Object,
            Options.Create(new LegalDocumentsOptions { TermsVersion = "1.0", PrivacyVersion = "1.0" }),
            Mock.Of<IEmailService>(),
            Options.Create(new AppOptions { FrontendBaseUrl = "http://localhost:3000" }),
            new EfUnitOfWork(context),
            NullLogger<AuthService>.Instance);

        return new Stack(auth, context);
    }

    private static RegisterRequest NewRegisterRequest(bool acceptedDataProcessing = true) => new()
    {
        Email = Email,
        Password = "Clave-segura-1",
        FirstName = "Lucía",
        LastName = "Martínez",
        Phone = "+34600111222",
        AcceptedTerms = true,
        AcceptedPrivacy = true,
        AcceptedTermsVersion = "1.0",
        AcceptedPrivacyVersion = "1.0",
        AcceptedDataProcessing = acceptedDataProcessing,
    };

    private async Task<AuthResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        using var stack = CreateStack();
        return await stack.Auth.RegisterAsync(request, OrgA, ipAddress: null);
    }

    private async Task<AuthResult<AuthResponse>> SocialLoginAsync()
    {
        using var stack = CreateStack();
        return await stack.Auth.ExternalLoginAsync(
            "Google", "google-lucia", Email, "Lucía", "Martínez", OrgA, ipAddress: null);
    }

    /// <summary>
    /// Ficha de otra cuenta que ya ocupa el email de Lucía en el centro: fuerza
    /// que la ficha nueva choque con el índice (OrganizationId, Email) DESPUÉS de
    /// que Identity haya guardado la cuenta, dentro de la transacción.
    /// </summary>
    private async Task SeedConflictingCustomerProfileAsync()
    {
        using var context = new AppDbContext(_options);
        context.Users.Add(new User
        {
            Id = 50,
            OrganizationId = OrgA,
            FirstName = "Otra",
            LastName = "Cuenta",
            Email = "otra@correo.com",
            NormalizedEmail = "OTRA@CORREO.COM",
            UserName = "otra@correo.com",
            NormalizedUserName = "OTRA@CORREO.COM",
            Rol = Roles.Customer,
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        context.Customers.Add(new Customer
        {
            Id = 50, OrganizationId = OrgA, FirstName = "Otra", LastName = "Cuenta", Email = Email,
        });
        await context.SaveChangesAsync();
    }

    // ── Registro local ────────────────────────────────────────────────────

    [Fact]
    public async Task El_registro_crea_cuenta_ficha_y_consentimiento_de_tratamiento_de_datos()
    {
        var result = await RegisterAsync(NewRegisterRequest());

        result.Success.Should().BeTrue(result.ErrorMessage);
        var id = result.Data!.User!.Id;

        using var check = new AppDbContext(_options);

        var ficha = await check.Customers.SingleAsync();
        ficha.Id.Should().Be(id, "la ficha comparte el Id de la cuenta");
        ficha.OrganizationId.Should().Be(OrgA);
        ficha.FirstName.Should().Be("Lucía");
        ficha.LastName.Should().Be("Martínez");
        ficha.Email.Should().Be(Email);
        ficha.Phone.Should().Be("+34600111222");
        ficha.Category.Should().Be(CustomerCategories.New);
        ficha.PreferredContactMethod.Should().Be(CustomerContactMethods.Email);
        ficha.IsActive.Should().BeTrue();

        // Solo lo que la persona ha marcado: tratamiento de datos, con fecha.
        var consentimiento = await check.CustomerConsents.SingleAsync();
        consentimiento.CustomerId.Should().Be(id);
        consentimiento.ConsentType.Should().Be(CustomerConsentTypes.DataProcessing);
        consentimiento.IsGranted.Should().BeTrue();
        consentimiento.GrantedAt.Should().NotBeNull();

        (await check.Users.SingleAsync()).Rol.Should().Be(Roles.Customer);
    }

    [Fact]
    public async Task Sin_aceptar_el_tratamiento_de_datos_no_hay_alta()
    {
        var result = await RegisterAsync(NewRegisterRequest(acceptedDataProcessing: false));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);

        using var check = new AppDbContext(_options);
        (await check.Users.CountAsync()).Should().Be(0);
        (await check.Customers.CountAsync()).Should().Be(0);
        (await check.CustomerConsents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Si_la_ficha_no_se_puede_guardar_la_cuenta_tampoco_queda()
    {
        await SeedConflictingCustomerProfileAsync();

        var act = () => RegisterAsync(NewRegisterRequest());

        await act.Should().ThrowAsync<DbUpdateException>();

        // Identity SÍ guardó la cuenta (su CreateAsync hace SaveChanges), pero
        // dentro de la transacción: se deshace con la ficha.
        using var check = new AppDbContext(_options);
        (await check.Users.AnyAsync(u => u.Email == Email)).Should().BeFalse();
        (await check.CustomerConsents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Un_email_ya_registrado_en_el_centro_es_conflicto_y_no_duplica_la_ficha()
    {
        (await RegisterAsync(NewRegisterRequest())).Success.Should().BeTrue();

        var repetido = await RegisterAsync(NewRegisterRequest());

        repetido.ErrorCode.Should().Be(ErrorCodes.GenConflict);

        using var check = new AppDbContext(_options);
        (await check.Customers.CountAsync()).Should().Be(1);
        (await check.CustomerConsents.CountAsync()).Should().Be(1);
    }

    // ── Alta social ───────────────────────────────────────────────────────

    [Fact]
    public async Task El_alta_social_crea_cuenta_vinculo_y_ficha_sin_consentimientos()
    {
        var result = await SocialLoginAsync();

        result.Success.Should().BeTrue(result.ErrorMessage);
        var id = result.Data!.User!.Id;

        using var check = new AppDbContext(_options);
        (await check.Users.SingleAsync()).EmailConfirmed.Should().BeTrue();
        (await check.UserLogins.SingleAsync()).UserId.Should().Be(id);

        var ficha = await check.Customers.SingleAsync();
        ficha.Id.Should().Be(id);
        ficha.Email.Should().Be(Email);
        ficha.Category.Should().Be(CustomerCategories.New);

        // El alta social no pasa por el formulario: no hay consentimiento que
        // guardar, y no se inventa ninguno.
        (await check.CustomerConsents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Volver_a_entrar_con_la_misma_cuenta_social_no_crea_otra_ficha()
    {
        var primera = await SocialLoginAsync();
        var segunda = await SocialLoginAsync();

        segunda.Data!.User!.Id.Should().Be(primera.Data!.User!.Id);

        using var check = new AppDbContext(_options);
        (await check.Customers.CountAsync()).Should().Be(1);
        (await check.UserLogins.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Vincular_el_proveedor_a_una_cuenta_existente_no_toca_las_fichas()
    {
        // Añadir ficha de clienta a una cuenta que ya existe en el centro (por
        // ejemplo, una empleada) es de RA-869d7f369, no del alta pública.
        var registro = await RegisterAsync(NewRegisterRequest());

        var social = await SocialLoginAsync();

        social.Data!.User!.Id.Should().Be(registro.Data!.User!.Id);

        using var check = new AppDbContext(_options);
        (await check.Customers.CountAsync()).Should().Be(1);
        (await check.UserLogins.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Si_la_ficha_del_alta_social_falla_no_quedan_cuenta_ni_vinculo()
    {
        await SeedConflictingCustomerProfileAsync();

        var act = () => SocialLoginAsync();

        await act.Should().ThrowAsync<DbUpdateException>();

        using var check = new AppDbContext(_options);
        (await check.Users.AnyAsync(u => u.Email == Email)).Should().BeFalse();
        (await check.UserLogins.CountAsync()).Should().Be(0);
    }
}

/// <summary>Validación de entrada del registro (RA-869f1xc2n).</summary>
public class RegisterRequestValidatorTests
{
    private static RegisterRequest ValidRequest(bool acceptedDataProcessing) => new()
    {
        Email = "lucia@correo.com",
        Password = "Clave-segura-1",
        FirstName = "Lucía",
        LastName = "Martínez",
        AcceptedTerms = true,
        AcceptedPrivacy = true,
        AcceptedTermsVersion = "1.0",
        AcceptedPrivacyVersion = "1.0",
        AcceptedDataProcessing = acceptedDataProcessing,
    };

    [Fact]
    public void El_tratamiento_de_datos_es_obligatorio_con_su_propio_checkbox()
    {
        var validator = new RegisterRequestValidator();

        validator.Validate(ValidRequest(acceptedDataProcessing: true)).IsValid.Should().BeTrue();

        validator.Validate(ValidRequest(acceptedDataProcessing: false)).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(RegisterRequest.AcceptedDataProcessing));
    }
}
