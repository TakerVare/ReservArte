using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Services;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Tests unitarios del emisor de tokens (vol. 2 §9.2.1): presencia de claims,
/// expiración y validación con clave simétrica de prueba. Sin I/O: el
/// servicio es una unidad pura que solo depende de JwtOptions.
/// </summary>
public class JwtTokenServiceTests
{
    // Clave de prueba: 48 bytes (holgada para HMAC-SHA256, que exige ≥ 32).
    // NO es un secreto real; solo sirve para firmar/validar en test.
    private const string TestSecretKey = "clave-de-prueba-para-tests-unitarios-jwt-0123456789";
    private const string TestIssuer = "https://test.reservarte.local";
    private const string TestAudience = "reservarte-test";

    private static JwtTokenService CreateService(
        int accessTokenMinutes = 60, int refreshTokenDays = 30)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = TestIssuer,
            Audience = TestAudience,
            SecretKey = TestSecretKey,
            AccessTokenMinutes = accessTokenMinutes,
            RefreshTokenDays = refreshTokenDays,
        });

        return new JwtTokenService(options);
    }

    private static User CreateUser() => new()
    {
        Id = 42,
        Email = "empleada@morethanbrows.com",
        Rol = "employee",
    };

    private static readonly Guid TestOrgId =
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    // Lee los claims de un JWT sin validarlo (para inspeccionar lo emitido)
    private static JwtSecurityToken ReadToken(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    // ── GenerateAccessToken: presencia de los cinco claims ────────────────

    [Fact]
    public void GenerateAccessToken_incluye_el_claim_sub_con_el_id_del_usuario()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateAccessToken(user, TestOrgId);

        var jwt = ReadToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "42");
    }

    [Fact]
    public void GenerateAccessToken_incluye_el_claim_email()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateAccessToken(user, TestOrgId);

        var jwt = ReadToken(token);
        jwt.Claims.Should().Contain(c =>
            c.Type == "email" && c.Value == "empleada@morethanbrows.com");
    }

    [Fact]
    public void GenerateAccessToken_incluye_el_claim_organization_id()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateAccessToken(user, TestOrgId);

        var jwt = ReadToken(token);
        jwt.Claims.Should().Contain(c =>
            c.Type == "organization_id" && c.Value == TestOrgId.ToString());
    }

    [Fact]
    public void GenerateAccessToken_incluye_el_rol_con_el_nombre_corto_role()
    {
        var service = CreateService();
        var user = CreateUser();

        var token = service.GenerateAccessToken(user, TestOrgId);

        var jwt = ReadToken(token);
        // El claim de rol se emite como "role" literal, no como la URI larga
        // de ClaimTypes.Role (regresión corregida durante el desarrollo).
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "employee");
    }

    [Fact]
    public void GenerateAccessToken_incluye_un_jti_unico_por_token()
    {
        var service = CreateService();
        var user = CreateUser();

        var jwt1 = ReadToken(service.GenerateAccessToken(user, TestOrgId));
        var jwt2 = ReadToken(service.GenerateAccessToken(user, TestOrgId));

        var jti1 = jwt1.Claims.Single(c => c.Type == "jti").Value;
        var jti2 = jwt2.Claims.Single(c => c.Type == "jti").Value;

        jti1.Should().NotBe(jti2);
    }

    [Fact]
    public void GenerateAccessToken_con_email_nulo_emite_el_claim_vacio_sin_lanzar()
    {
        var service = CreateService();
        var user = new User { Id = 7, Email = null, Rol = "admin" };

        var act = () => service.GenerateAccessToken(user, TestOrgId);

        act.Should().NotThrow();
    }

    // ── Expiración ────────────────────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_expira_segun_los_minutos_configurados()
    {
        var service = CreateService(accessTokenMinutes: 60);
        var user = CreateUser();
        var antes = DateTime.UtcNow;

        var jwt = ReadToken(service.GenerateAccessToken(user, TestOrgId));

        // La expiración cae ~60 min por delante (margen de 1 min por la
        // latencia entre capturar 'antes' y firmar el token)
        jwt.ValidTo.Should().BeCloseTo(antes.AddMinutes(60), TimeSpan.FromMinutes(1));
    }

    // ── ValidateToken ─────────────────────────────────────────────────────

    [Fact]
    public void ValidateToken_acepta_un_token_recien_emitido_y_recupera_los_claims()
    {
        var service = CreateService();
        var user = CreateUser();
        var token = service.GenerateAccessToken(user, TestOrgId);

        var principal = service.ValidateToken(token);

        principal.Should().NotBeNull();
        // MapInboundClaims=false: los claims se leen por su nombre original
        // (regresión que costó depurar, ahora blindada por este test)
        principal!.FindFirst("sub")!.Value.Should().Be("42");
        principal.FindFirst("role")!.Value.Should().Be("employee");
        principal.FindFirst("organization_id")!.Value.Should().Be(TestOrgId.ToString());
    }

    [Fact]
    public void ValidateToken_rechaza_un_token_manipulado()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken(CreateUser(), TestOrgId);

        // Alterar el último carácter invalida la firma
        var tampered = token[..^1] + (token[^1] == 'a' ? 'b' : 'a');

        var principal = service.ValidateToken(tampered);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_rechaza_un_token_firmado_con_otra_clave()
    {
        var emisor = CreateService();
        var token = emisor.GenerateAccessToken(CreateUser(), TestOrgId);

        // Otro servicio con distinta clave secreta: la firma no cuadra
        var otroServicio = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = TestIssuer,
            Audience = TestAudience,
            SecretKey = "otra-clave-completamente-distinta-para-el-test-987654",
            AccessTokenMinutes = 60,
            RefreshTokenDays = 30,
        }));

        var principal = otroServicio.ValidateToken(token);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_rechaza_un_token_caducado()
    {
        // Access token con expiración negativa: nace ya caducado
        var service = CreateService(accessTokenMinutes: -1);
        var token = service.GenerateAccessToken(CreateUser(), TestOrgId);

        var principal = service.ValidateToken(token);

        // ClockSkew = TimeSpan.Zero (sin tolerancia): la caducidad es exacta
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_devuelve_null_ante_una_cadena_sin_sentido()
    {
        var service = CreateService();

        var principal = service.ValidateToken("esto-no-es-un-jwt");

        principal.Should().BeNull();
    }

    // ── GenerateRefreshToken ──────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_produce_tokens_distintos_en_cada_llamada()
    {
        var service = CreateService();

        var t1 = service.GenerateRefreshToken();
        var t2 = service.GenerateRefreshToken();

        t1.Should().NotBe(t2);
    }

    [Fact]
    public void GenerateRefreshToken_devuelve_una_cadena_no_vacia()
    {
        var service = CreateService();

        var token = service.GenerateRefreshToken();

        token.Should().NotBeNullOrWhiteSpace();
    }

    // ── GenerateMfaTicket ─────────────────────────────────────────────────

    [Fact]
    public void GenerateMfaTicket_incluye_el_claim_mfa_pending()
    {
        var service = CreateService();
        var user = CreateUser();

        var jwt = ReadToken(service.GenerateMfaTicket(user, TestOrgId));

        jwt.Claims.Should().Contain(c => c.Type == "mfa_pending" && c.Value == "true");
    }

    [Fact]
    public void GenerateMfaTicket_NO_incluye_el_claim_role()
    {
        var service = CreateService();
        var user = CreateUser();

        var jwt = ReadToken(service.GenerateMfaTicket(user, TestOrgId));

        // Garantía de seguridad: el ticket no lleva rol, no autoriza operaciones
        jwt.Claims.Should().NotContain(c => c.Type == "role");
    }

    [Fact]
    public void GenerateMfaTicket_incluye_el_sub_del_usuario()
    {
        var service = CreateService();
        var user = CreateUser();

        var jwt = ReadToken(service.GenerateMfaTicket(user, TestOrgId));

        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "42");
    }
}