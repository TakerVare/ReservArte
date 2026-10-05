using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Configuración del centro (RA-869f74u7y): <c>GET|PUT /api/v1/organization/settings</c>
/// y la zona horaria que de ahí toma la disponibilidad. Cada test que escribe usa
/// un centro propio: la configuración es única por organización y cambiar la del
/// centro A movería los huecos de los demás tests.
/// </summary>
[Collection(ApiCollection.Name)]
public class OrganizationSettingsTests(ApiFactory factory)
{
    private const string Settings = "/api/v1/organization/settings";

    private static readonly DateOnly Invierno = new(2030, 1, 14);

    private async Task<string> TokenAsync(Guid organizationId, string rol)
    {
        var caller = await factory.CreateEmployeeAsync(organizationId, rol);
        return await factory.TokenForAsync(organizationId, caller.Id);
    }

    private Task<ApiResult> Put(Guid organizationId, string? token, object body) =>
        factory.SendAsync(HttpMethod.Put, Settings, organizationId, token, body);

    private static object Body(string timeZone = "Atlantic/Canary", int hours = 48, int noShows = 5) =>
        new { timeZone, cancellationHoursThreshold = hours, maxNoShowsBeforeBlock = noShows };

    // ── Lectura ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Un_centro_sin_configuracion_guardada_recibe_los_valores_por_defecto()
    {
        var org = await factory.CreateOrganizationAsync();

        var result = await factory.SendAsync(HttpMethod.Get, Settings, org, await TokenAsync(org, Roles.Admin));

        result.Status.Should().Be(HttpStatusCode.OK);
        result.ShouldBeEnvelope(success: true);
        result.Data.GetProperty("timeZone").GetString().Should().Be("Europe/Madrid");
        result.Data.GetProperty("cancellationHoursThreshold").GetInt32().Should().Be(24);
        result.Data.GetProperty("maxNoShowsBeforeBlock").GetInt32().Should().Be(3);
        result.Data.GetProperty("updatedAt").ValueKind.Should().Be(JsonValueKind.Null);
        (await RowsAsync(org)).Should().BeEmpty("leer no crea la fila");
    }

    [Fact]
    public async Task El_centro_piloto_nace_con_su_fila_de_configuracion()
    {
        (await RowsAsync(TestData.OrgA)).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(
                new { TimeZone = "Europe/Madrid", CancellationHoursThreshold = 24, MaxNoShowsBeforeBlock = 3 });
    }

    // ── Escritura ─────────────────────────────────────────────────────────

    [Fact]
    public async Task El_primer_PUT_crea_la_fila_y_el_segundo_la_actualiza()
    {
        var org = await factory.CreateOrganizationAsync();
        var token = await TokenAsync(org, Roles.Manager);

        var created = await Put(org, token, Body());

        created.Status.Should().Be(HttpStatusCode.OK);
        created.ShouldBeEnvelope(success: true);
        created.Data.GetProperty("timeZone").GetString().Should().Be("Atlantic/Canary");
        created.Data.GetProperty("cancellationHoursThreshold").GetInt32().Should().Be(48);
        created.Data.GetProperty("maxNoShowsBeforeBlock").GetInt32().Should().Be(5);

        var updated = await Put(org, token, Body("Europe/Lisbon", hours: 0, noShows: 1));

        updated.Status.Should().Be(HttpStatusCode.OK);
        updated.Data.GetProperty("updatedAt").GetString().Should().EndWith("Z");

        var read = await factory.SendAsync(HttpMethod.Get, Settings, org, token);
        read.Data.GetProperty("timeZone").GetString().Should().Be("Europe/Lisbon");
        read.Data.GetProperty("cancellationHoursThreshold").GetInt32().Should().Be(0);
        read.Data.GetProperty("maxNoShowsBeforeBlock").GetInt32().Should().Be(1);

        (await RowsAsync(org)).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(
                new { OrganizationId = org, TimeZone = "Europe/Lisbon", CancellationHoursThreshold = 0, MaxNoShowsBeforeBlock = 1 });
    }

    [Fact]
    public async Task La_configuracion_de_un_centro_no_toca_la_de_otro()
    {
        var one = await factory.CreateOrganizationAsync();
        var other = await factory.CreateOrganizationAsync();
        (await Put(other, await TokenAsync(other, Roles.Admin), Body("Europe/Lisbon", 12, 2)))
            .Status.Should().Be(HttpStatusCode.OK);

        (await Put(one, await TokenAsync(one, Roles.Admin), Body())).Status.Should().Be(HttpStatusCode.OK);

        (await RowsAsync(other)).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(
                new { TimeZone = "Europe/Lisbon", CancellationHoursThreshold = 12, MaxNoShowsBeforeBlock = 2 });
        (await RowsAsync(TestData.OrgA)).Should().ContainSingle().Which.TimeZone.Should().Be("Europe/Madrid");
    }

    [Fact]
    public async Task Un_token_de_otro_centro_no_escribe_la_configuracion()
    {
        var org = await factory.CreateOrganizationAsync();
        var foreign = await TokenAsync(TestData.OrgB, Roles.Admin);

        var result = await Put(org, foreign, Body());

        result.Status.Should().Be(HttpStatusCode.Forbidden);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.OrgTenantMismatch);
        (await RowsAsync(org)).Should().BeEmpty();
    }

    // ── Autorización ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    public async Task Sin_sesion_responde_401(string method)
    {
        var result = await factory.SendAsync(new HttpMethod(method), Settings, TestData.OrgA, token: null, Body());

        result.Status.Should().Be(HttpStatusCode.Unauthorized);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenUnauthorized);
    }

    [Theory]
    [InlineData(Roles.Employee, "GET")]
    [InlineData(Roles.Employee, "PUT")]
    [InlineData(Roles.Customer, "GET")]
    [InlineData(Roles.Customer, "PUT")]
    public async Task Empleadas_y_clientas_reciben_403(string rol, string method)
    {
        var org = await factory.CreateOrganizationAsync();
        var token = rol == Roles.Customer
            ? await factory.TokenForAsync(org, (await factory.CreateCustomerAsync(org)).Id)
            : await TokenAsync(org, rol);

        var result = await factory.SendAsync(new HttpMethod(method), Settings, org, token, Body());

        result.Status.Should().Be(HttpStatusCode.Forbidden);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
        (await RowsAsync(org)).Should().BeEmpty();
    }

    // ── Validación ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Europa/Madrid")]
    [InlineData("Romance Standard Time")]
    public async Task Una_zona_horaria_desconocida_da_400_UnknownTimeZone(string timeZone)
    {
        var org = await factory.CreateOrganizationAsync();

        var result = await Put(org, await TokenAsync(org, Roles.Admin), Body(timeZone));

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        var detail = result.Body.GetProperty("error").GetProperty("details").EnumerateArray().Single();
        detail.GetProperty("field").GetString().Should().Be("timeZone");
        detail.GetProperty("code").GetString().Should().Be("UnknownTimeZone");
        (await RowsAsync(org)).Should().BeEmpty();
    }

    [Fact]
    public async Task Un_cuerpo_incompleto_o_fuera_de_rango_da_400_con_un_detalle_por_campo()
    {
        var org = await factory.CreateOrganizationAsync();
        var token = await TokenAsync(org, Roles.Admin);

        var missing = await Put(org, token, new { timeZone = "Europe/Madrid" });
        var outOfRange = await Put(org, token, Body("Europe/Madrid", hours: 721, noShows: 0));

        foreach (var result in new[] { missing, outOfRange })
        {
            result.Status.Should().Be(HttpStatusCode.BadRequest);
            result.ShouldBeEnvelope(success: false);
            result.Body.GetProperty("error").GetProperty("details").EnumerateArray()
                .Select(d => d.GetProperty("field").GetString())
                .Should().BeEquivalentTo("cancellationHoursThreshold", "maxNoShowsBeforeBlock");
        }

        (await RowsAsync(org)).Should().BeEmpty();
    }

    // ── La zona configurada es la que usa la agenda ───────────────────────

    [Fact]
    public async Task Con_el_centro_en_Canarias_una_ausencia_de_10_a_11_bloquea_las_10_canarias_y_no_las_11()
    {
        var org = await factory.CreateOrganizationAsync();
        (await Put(org, await TokenAsync(org, Roles.Admin), Body("Atlantic/Canary"))).Status.Should().Be(HttpStatusCode.OK);
        var employee = await factory.CreateEmployeeAsync(org);

        // 10:00-11:00 en Canarias en invierno (UTC+0). Con la zona de la península
        // fija, esta ausencia caía de 11:00 a 12:00 y el resultado era el contrario.
        await AddExceptionAsync(org, employee,
            Invierno.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
            Invierno.ToDateTime(new TimeOnly(11, 0), DateTimeKind.Utc));

        (await IsFreeAsync(org, employee, 10, 0, 10, 45)).Should().BeFalse();
        (await IsFreeAsync(org, employee, 11, 0, 11, 45)).Should().BeTrue();
    }

    [Fact]
    public async Task Sin_configuracion_la_misma_ausencia_se_lee_en_hora_peninsular()
    {
        var org = await factory.CreateOrganizationAsync();
        var employee = await factory.CreateEmployeeAsync(org);

        // 10:00Z-11:00Z = 11:00-12:00 en Madrid (invierno, UTC+1).
        await AddExceptionAsync(org, employee,
            Invierno.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc),
            Invierno.ToDateTime(new TimeOnly(11, 0), DateTimeKind.Utc));

        (await IsFreeAsync(org, employee, 10, 0, 10, 45)).Should().BeTrue();
        (await IsFreeAsync(org, employee, 11, 0, 11, 45)).Should().BeFalse();
    }

    // ── Andamiaje ─────────────────────────────────────────────────────────

    private async Task<List<OrganizationSettings>> RowsAsync(Guid organizationId)
    {
        await using var scope = factory.CreateTenantScope(organizationId);
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .OrganizationSettings.AsNoTracking().ToListAsync();
    }

    private async Task AddExceptionAsync(Guid organizationId, Employee employee, DateTime startUtc, DateTime endUtc)
    {
        await using var scope = factory.CreateTenantScope(organizationId);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.EmployeeExceptions.Add(new EmployeeException
        {
            OrganizationId = organizationId,
            EmployeeId = employee.Id,
            StartDateTime = startUtc,
            EndDateTime = endUtc,
            Type = EmployeeExceptionTypes.Vacation,
        });
        await db.SaveChangesAsync();
    }

    private async Task<bool> IsFreeAsync(
        Guid organizationId, Employee employee, int startHour, int startMinute, int endHour, int endMinute)
    {
        await using var scope = factory.CreateTenantScope(organizationId);
        var result = await scope.ServiceProvider.GetRequiredService<IAvailabilityService>()
            .EnsureSlotAvailableAsync(
                employee.Id, Invierno, new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));

        if (!result.Success)
        {
            result.ErrorCode.Should().Be(ErrorCodes.AptSlotUnavailable);
        }

        return result.Success;
    }
}
