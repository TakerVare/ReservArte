using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Auth;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Services;
using ReservArte.Shared.Api;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Email único por organización en los flujos de autenticación (RA-869f1xc0u):
/// la misma persona, con el mismo email, tiene una cuenta en cada centro, y cada
/// operación resuelve la cuenta de la organización de la petición.
///
/// Contra SQLite real, con el UserManager y el store de la API sobre un
/// AppDbContext construido con el tenant, como en una petición. Solo se simulan
/// los colaboradores externos (JWT, CAPTCHA, correo) y los proveedores de tokens.
/// </summary>
public class AuthServiceTenantTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private const string Email = "lucia@correo.com";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly List<EmailMessage> _sentEmails = new();

    public AuthServiceTenantTests()
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
        public Tenant(Guid organizationId) => OrganizationId = organizationId;

        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => true;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;
    }

    /// <summary>
    /// Proveedor de tokens determinista: el token identifica propósito, cuenta y
    /// security stamp, así que el de una cuenta no vale para otra.
    /// </summary>
    private sealed class FakeTokenProvider : IUserTwoFactorTokenProvider<User>
    {
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> manager, User user) =>
            Task.FromResult(false);

        public Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user) =>
            Task.FromResult(Token(purpose, user));

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user) =>
            Task.FromResult(token == Token(purpose, user));

        private static string Token(string purpose, User user) => $"{purpose}:{user.Id}:{user.SecurityStamp}";
    }

    private sealed record Stack(AuthService Auth, UserManager<User> Users, AppDbContext Context) : IDisposable
    {
        public void Dispose() => Context.Dispose();
    }

    /// <summary>Pila de una petición resuelta en la organización indicada.</summary>
    private Stack For(Guid organizationId)
    {
        var context = new AppDbContext(_options, new Tenant(organizationId));

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
        users.RegisterTokenProvider(TokenOptions.DefaultProvider, new FakeTokenProvider());
        users.RegisterTokenProvider(InvitationTokenDefaults.ProviderName, new FakeTokenProvider());

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<Guid>()))
            .Returns((User user, Guid organization) => $"access:{user.Id}:{organization}");
        jwt.Setup(j => j.GenerateRefreshToken()).Returns(() => Guid.NewGuid().ToString());

        var captcha = new Mock<ICaptchaService>();
        captcha.Setup(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>())).ReturnsAsync(true);

        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback((EmailMessage message, CancellationToken _) => _sentEmails.Add(message))
            .Returns(Task.CompletedTask);

        var auth = new AuthService(
            users,
            jwt.Object,
            context,
            Options.Create(new JwtOptions { RefreshTokenDays = 30 }),
            captcha.Object,
            Options.Create(new LegalDocumentsOptions { TermsVersion = "1.0", PrivacyVersion = "1.0" }),
            email.Object,
            Options.Create(new AppOptions { FrontendBaseUrl = "http://localhost:3000" }),
            new EfUnitOfWork(context),
            NullLogger<AuthService>.Instance);

        return new Stack(auth, users, context);
    }

    private async Task<int> RegisterAsync(Guid organizationId, string password)
    {
        using var stack = For(organizationId);

        var result = await stack.Auth.RegisterAsync(new RegisterRequest
        {
            Email = Email,
            Password = password,
            FirstName = "Lucía",
            LastName = "Martínez",
            AcceptedTerms = true,
            AcceptedPrivacy = true,
            AcceptedTermsVersion = "1.0",
            AcceptedPrivacyVersion = "1.0",
            AcceptedDataProcessing = true,
        }, organizationId, ipAddress: null);

        result.Success.Should().BeTrue(result.ErrorMessage);
        return result.Data!.User!.Id;
    }

    private async Task<AuthResult<AuthResponse>> LoginAsync(Guid organizationId, string password)
    {
        using var stack = For(organizationId);

        return await stack.Auth.LoginAsync(
            new LoginRequest { Email = Email, Password = password }, organizationId, ipAddress: null);
    }

    // ── Registro y login local ────────────────────────────────────────────

    [Fact]
    public async Task El_mismo_email_se_registra_en_dos_organizaciones()
    {
        var cuentaA = await RegisterAsync(OrgA, "Clave-de-A-1");
        var cuentaB = await RegisterAsync(OrgB, "Clave-de-B-1");

        cuentaA.Should().NotBe(cuentaB);

        using var check = new AppDbContext(_options);
        (await check.Users.Where(u => u.Email == Email).Select(u => u.OrganizationId).ToListAsync())
            .Should().BeEquivalentTo(new[] { OrgA, OrgB });
    }

    [Fact]
    public async Task Un_email_repetido_en_la_misma_organizacion_es_conflicto()
    {
        await RegisterAsync(OrgA, "Clave-de-A-1");

        using var stack = For(OrgA);
        var repetido = await stack.Auth.RegisterAsync(new RegisterRequest
        {
            Email = Email,
            Password = "Otra-clave-1",
            FirstName = "Lucía",
            LastName = "Duplicada",
            AcceptedTermsVersion = "1.0",
            AcceptedPrivacyVersion = "1.0",
            AcceptedDataProcessing = true,
        }, OrgA, ipAddress: null);

        repetido.Success.Should().BeFalse();
        repetido.ErrorCode.Should().Be(ErrorCodes.GenConflict);
    }

    [Fact]
    public async Task El_login_con_el_mismo_email_entra_en_la_cuenta_de_cada_organizacion()
    {
        var cuentaA = await RegisterAsync(OrgA, "Clave-de-A-1");
        var cuentaB = await RegisterAsync(OrgB, "Clave-de-B-1");

        (await LoginAsync(OrgA, "Clave-de-A-1")).Data!.User!.Id.Should().Be(cuentaA);
        (await LoginAsync(OrgB, "Clave-de-B-1")).Data!.User!.Id.Should().Be(cuentaB);

        // La contraseña de un centro no abre la cuenta del otro.
        (await LoginAsync(OrgA, "Clave-de-B-1")).ErrorCode.Should().Be(ErrorCodes.AuthInvalidCredentials);
    }

    // ── Login social ──────────────────────────────────────────────────────

    [Fact]
    public async Task La_misma_cuenta_del_proveedor_entra_en_dos_organizaciones()
    {
        // En OrgB ya había cuenta local con ese email: se vincula a ella. En OrgA
        // no había: se crea una cuenta solo-social.
        var cuentaLocalB = await RegisterAsync(OrgB, "Clave-de-B-1");

        async Task<int> SocialAsync(Guid organizationId)
        {
            using var stack = For(organizationId);
            var result = await stack.Auth.ExternalLoginAsync(
                "Google", "google-lucia", Email, "Lucía", "Martínez", organizationId, ipAddress: null);

            result.Success.Should().BeTrue(result.ErrorMessage);
            return result.Data!.User!.Id;
        }

        var cuentaA = await SocialAsync(OrgA);
        var cuentaB = await SocialAsync(OrgB);

        cuentaB.Should().Be(cuentaLocalB);
        cuentaA.Should().NotBe(cuentaB);

        // Segunda vez: cada centro resuelve su cuenta por el vínculo ya creado.
        (await SocialAsync(OrgA)).Should().Be(cuentaA);
        (await SocialAsync(OrgB)).Should().Be(cuentaB);

        using var check = new AppDbContext(_options);
        (await check.UserLogins.Where(l => l.ProviderKey == "google-lucia")
                .Select(l => new { l.UserId, l.OrganizationId }).ToListAsync())
            .Should().BeEquivalentTo(new[]
            {
                new { UserId = cuentaA, OrganizationId = OrgA },
                new { UserId = cuentaB, OrganizationId = OrgB },
            });
    }

    // ── Recuperación e invitación ─────────────────────────────────────────

    [Fact]
    public async Task Olvide_mi_contrasena_envia_el_enlace_de_la_cuenta_de_la_organizacion()
    {
        await RegisterAsync(OrgA, "Clave-de-A-1");
        var cuentaB = await RegisterAsync(OrgB, "Clave-de-B-1");

        using (var stack = For(OrgB))
        {
            await stack.Auth.ForgotPasswordAsync(Email, OrgB);
        }

        // El token del enlace es el de la cuenta de OrgB (propósito:id:stamp,
        // codificado para URL).
        _sentEmails.Should().ContainSingle().Which.Body.Should().Contain($"ResetPassword%3A{cuentaB}%3A");
    }

    [Fact]
    public async Task El_reset_cambia_solo_la_contrasena_de_la_cuenta_de_su_organizacion()
    {
        await RegisterAsync(OrgA, "Clave-comun-1");
        await RegisterAsync(OrgB, "Clave-comun-1");

        string tokenA;
        using (var stack = For(OrgA))
        {
            tokenA = await stack.Users.GeneratePasswordResetTokenAsync((await stack.Users.FindByEmailAsync(Email))!);
        }

        var request = new ResetPasswordRequest { Email = Email, Token = tokenA, NewPassword = "Clave-nueva-1" };

        // El token de la cuenta de OrgA no vale en OrgB, aunque el email coincida.
        using (var stack = For(OrgB))
        {
            (await stack.Auth.ResetPasswordAsync(request, OrgB)).ErrorCode.Should().Be(ErrorCodes.AuthInvalidCredentials);
        }

        using (var stack = For(OrgA))
        {
            (await stack.Auth.ResetPasswordAsync(request, OrgA)).Success.Should().BeTrue();
        }

        (await LoginAsync(OrgA, "Clave-nueva-1")).Success.Should().BeTrue();
        (await LoginAsync(OrgB, "Clave-comun-1")).Success.Should().BeTrue();
        (await LoginAsync(OrgB, "Clave-nueva-1")).Success.Should().BeFalse();
    }

    [Fact]
    public async Task La_invitacion_establece_la_contrasena_de_la_cuenta_de_su_organizacion()
    {
        // Cuentas sin contraseña en los dos centros, como tras un alta de empleada.
        string tokenA;
        using (var stack = For(OrgA))
        {
            var cuentaA = new User
            {
                OrganizationId = OrgA, FirstName = "Lucía", LastName = "A", Email = Email, UserName = Email,
                Rol = Roles.Employee,
            };
            (await stack.Users.CreateAsync(cuentaA)).Succeeded.Should().BeTrue();
            tokenA = await stack.Users.GenerateUserTokenAsync(
                cuentaA, InvitationTokenDefaults.ProviderName, InvitationTokenDefaults.Purpose);
        }

        using (var stack = For(OrgB))
        {
            (await stack.Users.CreateAsync(new User
            {
                OrganizationId = OrgB, FirstName = "Lucía", LastName = "B", Email = Email, UserName = Email,
                Rol = Roles.Employee,
            })).Succeeded.Should().BeTrue();
        }

        var request = new SetPasswordRequest { Email = Email, Token = tokenA, NewPassword = "Clave-nueva-1" };

        using (var stack = For(OrgB))
        {
            (await stack.Auth.SetPasswordAsync(request, OrgB)).ErrorCode.Should().Be(ErrorCodes.AuthInvalidCredentials);
        }

        using (var stack = For(OrgA))
        {
            (await stack.Auth.SetPasswordAsync(request, OrgA)).Success.Should().BeTrue();
        }

        using var check = new AppDbContext(_options);
        (await check.Users.Where(u => u.Email == Email).ToDictionaryAsync(u => u.OrganizationId, u => u.PasswordHash != null))
            .Should().BeEquivalentTo(new Dictionary<Guid, bool> { [OrgA] = true, [OrgB] = false });
    }
}
