using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using ReservArte.Domain.Entities;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Ficha completa de la clienta (4.2b): consentimientos (retirar el de datos da de
/// baja la ficha, H-47), alergias y bloqueo. Que una clienta bloqueada no reserve ya
/// lo cubre AppointmentsContractTests.
/// </summary>
[Collection(ApiCollection.Name)]
public class CustomerRecordTests(ApiFactory factory)
{
    private const string Customers = "/api/v1/customers";

    private async Task<string> TokenAsync(string rol)
    {
        var caller = await factory.CreateEmployeeAsync(TestData.OrgA, rol);
        return await factory.TokenForAsync(TestData.OrgA, caller.Id);
    }

    private Task<ApiResult> Send(string token, HttpMethod method, string path, object? body = null) =>
        factory.SendAsync(method, path, TestData.OrgA, token, body);

    private static JsonElement Consent(ApiResult result, string type) =>
        result.Data.GetProperty("consents").EnumerateArray()
            .Single(c => c.GetProperty("consentType").GetString() == type);

    // ── Consentimientos ───────────────────────────────────────────────────

    [Fact]
    public async Task Dar_y_retirar_un_consentimiento_guarda_sus_fechas_y_no_toca_la_ficha()
    {
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var token = await TokenAsync(Roles.Manager);
        var path = $"{Customers}/{customer.Id}/consents/{CustomerConsentTypes.Marketing}";

        var granted = await Send(token, HttpMethod.Put, path, new { granted = true });
        var revoked = await Send(token, HttpMethod.Put, path, new { granted = false });

        granted.Status.Should().Be(HttpStatusCode.OK);
        granted.ShouldBeEnvelope(success: true);
        Consent(granted, CustomerConsentTypes.Marketing).GetProperty("isGranted").GetBoolean().Should().BeTrue();
        var afterRevoke = Consent(revoked, CustomerConsentTypes.Marketing);
        afterRevoke.GetProperty("isGranted").GetBoolean().Should().BeFalse();
        afterRevoke.GetProperty("revokedAt").GetString().Should().NotBeNull();
        afterRevoke.GetProperty("grantedAt").GetString().Should()
            .Be(Consent(granted, CustomerConsentTypes.Marketing).GetProperty("grantedAt").GetString());
        revoked.Data.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Retirar_el_consentimiento_de_datos_da_de_baja_la_ficha()
    {
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var token = await TokenAsync(Roles.Admin);
        var path = $"{Customers}/{customer.Id}/consents/{CustomerConsentTypes.DataProcessing}";
        await Send(token, HttpMethod.Put, path, new { granted = true });

        var result = await Send(token, HttpMethod.Put, path, new { granted = false });

        result.Status.Should().Be(HttpStatusCode.OK);
        result.Data.GetProperty("isActive").GetBoolean().Should().BeFalse();
        Consent(result, CustomerConsentTypes.DataProcessing).GetProperty("isGranted").GetBoolean().Should().BeFalse();
        var list = await Send(token, HttpMethod.Get, $"{Customers}?search={customer.Email}");
        list.Data.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Una_finalidad_desconocida_da_400_y_una_empleada_no_gestiona_consentimientos()
    {
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);

        var unknown = await Send(await TokenAsync(Roles.Admin), HttpMethod.Put,
            $"{Customers}/{customer.Id}/consents/telepatia", new { granted = true });
        var byEmployee = await Send(await TokenAsync(Roles.Employee), HttpMethod.Put,
            $"{Customers}/{customer.Id}/consents/{CustomerConsentTypes.Marketing}", new { granted = true });

        unknown.Status.Should().Be(HttpStatusCode.BadRequest);
        unknown.ShouldBeEnvelope(success: false);
        unknown.Body.GetProperty("error").GetProperty("details")[0].GetProperty("code").GetString()
            .Should().Be("UnknownConsent");
        byEmployee.Status.Should().Be(HttpStatusCode.Forbidden);
        byEmployee.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
    }

    // ── Alergias ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Una_empleada_da_de_alta_edita_y_retira_una_alergia()
    {
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var token = await TokenAsync(Roles.Employee);
        var allergies = $"{Customers}/{customer.Id}/allergies";

        var created = await Send(token, HttpMethod.Post, allergies,
            new { allergyDescription = "  Tinte PPD  ", severity = AllergySeverities.High });
        created.Status.Should().Be(HttpStatusCode.Created);
        created.ShouldBeEnvelope(success: true);
        var id = created.Data.GetProperty("id").GetInt32();
        created.Data.GetProperty("allergyDescription").GetString().Should().Be("Tinte PPD");

        var updated = await Send(token, HttpMethod.Put, $"{allergies}/{id}",
            new { allergyDescription = "Tinte PPD y níquel", severity = AllergySeverities.Medium });
        updated.Data.GetProperty("severity").GetString().Should().Be(AllergySeverities.Medium);

        var deleted = await Send(token, HttpMethod.Delete, $"{allergies}/{id}");
        var again = await Send(token, HttpMethod.Delete, $"{allergies}/{id}");
        var profile = await Send(token, HttpMethod.Get, $"{Customers}/{customer.Id}");
        var editRetired = await Send(token, HttpMethod.Put, $"{allergies}/{id}",
            new { allergyDescription = "Otra", severity = AllergySeverities.Low });

        deleted.Status.Should().Be(HttpStatusCode.OK);
        again.Status.Should().Be(HttpStatusCode.OK);
        profile.Data.GetProperty("allergies").GetArrayLength().Should().Be(0);
        editRetired.Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Una_alergia_de_otra_clienta_da_404_y_una_gravedad_desconocida_400()
    {
        var owner = await factory.CreateCustomerAsync(TestData.OrgA);
        var other = await factory.CreateCustomerAsync(TestData.OrgA);
        var token = await TokenAsync(Roles.Manager);
        var created = await Send(token, HttpMethod.Post, $"{Customers}/{owner.Id}/allergies",
            new { allergyDescription = "Látex", severity = AllergySeverities.Low });
        var id = created.Data.GetProperty("id").GetInt32();

        var foreign = await Send(token, HttpMethod.Delete, $"{Customers}/{other.Id}/allergies/{id}");
        var invalid = await Send(token, HttpMethod.Post, $"{Customers}/{owner.Id}/allergies",
            new { allergyDescription = "Látex", severity = "letal" });

        foreign.Status.Should().Be(HttpStatusCode.NotFound);
        foreign.ShouldBeEnvelope(success: false);
        invalid.Status.Should().Be(HttpStatusCode.BadRequest);
        invalid.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
    }

    // ── Bloqueo ───────────────────────────────────────────────────────────

    [Fact]
    public async Task La_gerencia_bloquea_con_motivo_y_desbloquea_y_una_empleada_no()
    {
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var manager = await TokenAsync(Roles.Manager);

        var noReason = await Send(manager, HttpMethod.Post, $"{Customers}/{customer.Id}/block", new { reason = " " });
        var blocked = await Send(manager, HttpMethod.Post, $"{Customers}/{customer.Id}/block",
            new { reason = "  Impagos  " });
        var byEmployee = await Send(await TokenAsync(Roles.Employee), HttpMethod.Post,
            $"{Customers}/{customer.Id}/unblock");
        var unblocked = await Send(manager, HttpMethod.Post, $"{Customers}/{customer.Id}/unblock");

        noReason.Status.Should().Be(HttpStatusCode.BadRequest);
        blocked.Status.Should().Be(HttpStatusCode.OK);
        blocked.ShouldBeEnvelope(success: true);
        blocked.Data.GetProperty("isBlocked").GetBoolean().Should().BeTrue();
        blocked.Data.GetProperty("blockedReason").GetString().Should().Be("Impagos");
        byEmployee.Status.Should().Be(HttpStatusCode.Forbidden);
        unblocked.Data.GetProperty("isBlocked").GetBoolean().Should().BeFalse();
        unblocked.Data.GetProperty("blockedReason").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task No_se_toca_la_ficha_de_una_clienta_de_otro_centro()
    {
        var foreign = await factory.CreateCustomerAsync(TestData.OrgB);
        var token = await TokenAsync(Roles.Admin);

        var block = await Send(token, HttpMethod.Post, $"{Customers}/{foreign.Id}/block", new { reason = "x" });
        var consent = await Send(token, HttpMethod.Put,
            $"{Customers}/{foreign.Id}/consents/{CustomerConsentTypes.Marketing}", new { granted = true });
        var allergy = await Send(token, HttpMethod.Post, $"{Customers}/{foreign.Id}/allergies",
            new { allergyDescription = "Látex", severity = AllergySeverities.Low });

        foreach (var result in new[] { block, consent, allergy })
        {
            result.Status.Should().Be(HttpStatusCode.NotFound);
            result.ShouldBeEnvelope(success: false);
        }
    }
}
