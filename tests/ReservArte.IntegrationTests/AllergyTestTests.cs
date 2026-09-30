using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Prueba de alergia previa (RA-869f9cu2x, H-41): el personal registra en la ficha de
/// la clienta cuándo la pasó, y la cita de un servicio que la exige avisa (sin
/// bloquear) si falta o no llega a las horas de antelación. No caduca.
/// </summary>
[Collection(ApiCollection.Name)]
public class AllergyTestTests(ApiFactory factory)
{
    // Para medir la antelación, la cita va en el pasado (la prueba no puede ser futura y
    // el personal puede registrar citas pasadas): lunes 2025-01-13 a las 10:00 en Madrid
    // (invierno, UTC+1), 09:00Z. El tinte exige la prueba 48 h antes: 2025-01-11T09:00:00Z.
    private static readonly DateOnly PastDay = new(2025, 1, 13);
    private static readonly DateTime AppointmentStartUtc = new(2025, 1, 13, 9, 0, 0, DateTimeKind.Utc);

    // ── Registro en la ficha ──────────────────────────────────────────────

    [Fact]
    public async Task Una_empleada_registra_la_prueba_y_la_ficha_la_devuelve_en_UTC()
    {
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var token = await StaffTokenAsync(Roles.Employee);

        var recorded = await RecordAsync(token, customer.Id, "2026-09-15T10:30:00+02:00");
        var detail = await factory.SendAsync(HttpMethod.Get, $"/api/v1/customers/{customer.Id}", TestData.OrgA, token);

        recorded.Status.Should().Be(HttpStatusCode.OK);
        recorded.ShouldBeEnvelope(success: true);
        recorded.Data.GetProperty("lastAllergyTestAt").GetString().Should().Be("2026-09-15T08:30:00Z");
        detail.Data.GetProperty("lastAllergyTestAt").GetString().Should().Be("2026-09-15T08:30:00Z");
    }

    [Theory]
    [InlineData("2099-01-01T10:00:00Z", "InFuture")]
    [InlineData("2026-09-15T10:30:00", "MissingTimeZone")]
    public async Task Una_fecha_futura_o_sin_zona_da_400_en_testedAt(string testedAt, string code)
    {
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var token = await StaffTokenAsync(Roles.Employee);

        var result = await RecordAsync(token, customer.Id, testedAt);

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        var detail = result.Body.GetProperty("error").GetProperty("details").EnumerateArray().Single();
        detail.GetProperty("field").GetString().Should().Be("testedAt");
        detail.GetProperty("code").GetString().Should().Be(code);
    }

    [Fact]
    public async Task Una_clienta_no_registra_pruebas_y_la_de_otro_centro_da_404()
    {
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var customerB = await factory.CreateCustomerAsync(TestData.OrgB);
        var customerToken = await factory.TokenForAsync(TestData.OrgA, customer.Id);
        var staff = await StaffTokenAsync(Roles.Admin);

        var byCustomer = await RecordAsync(customerToken, customer.Id, "2026-09-15T10:30:00Z");
        var foreign = await RecordAsync(staff, customerB.Id, "2026-09-15T10:30:00Z");

        byCustomer.Status.Should().Be(HttpStatusCode.Forbidden);
        foreign.Status.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Avisos en la cita ─────────────────────────────────────────────────

    [Fact]
    public async Task Sin_prueba_la_cita_se_crea_con_el_aviso_AllergyTestMissing()
    {
        var scene = await SceneRequiringTestAsync();
        var token = await StaffTokenAsync(Roles.Employee);

        var created = await BookTintAsync(token, scene);
        var detail = await factory.SendAsync(HttpMethod.Get,
            $"/api/v1/appointments/{created.Data.GetProperty("id").GetInt32()}", TestData.OrgA, token);

        created.Status.Should().Be(HttpStatusCode.Created);
        foreach (var data in new[] { created.Data, detail.Data })
        {
            var warning = Warnings(data).Single();
            warning.GetProperty("code").GetString().Should().Be("AllergyTestMissing");
            warning.GetProperty("serviceId").GetInt32().Should().Be(scene.Tint.Id);
        }
    }

    [Theory]
    [InlineData(47, "AllergyTestTooLate")]
    [InlineData(48, null)]
    [InlineData(24 * 400, null)]
    public async Task La_prueba_vale_si_llega_a_las_horas_del_servicio_y_no_caduca(int hoursBefore, string? expected)
    {
        var scene = await SceneRequiringTestAsync();
        var token = await StaffTokenAsync(Roles.Employee);
        var testedAt = AppointmentStartUtc.AddHours(-hoursBefore);
        (await RecordAsync(token, scene.Customer.Id, testedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")))
            .Status.Should().Be(HttpStatusCode.OK);

        var created = await BookTintAsync(token, scene, PastDay);

        created.Status.Should().Be(HttpStatusCode.Created);
        Warnings(created.Data).Select(w => w.GetProperty("code").GetString())
            .Should().Equal(expected is null ? [] : [expected]);
        var detail = await factory.SendAsync(HttpMethod.Get,
            $"/api/v1/appointments/{created.Data.GetProperty("id").GetInt32()}", TestData.OrgA, token);
        Warnings(detail.Data).Select(w => w.GetProperty("code").GetString())
            .Should().Equal(expected is null ? [] : [expected]);
    }

    [Fact]
    public async Task Un_servicio_que_no_exige_prueba_no_avisa()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await StaffTokenAsync(Roles.Employee);

        var created = await BookTintAsync(token, scene);

        Warnings(created.Data).Should().BeEmpty();
    }

    // ── Ayudantes ─────────────────────────────────────────────────────────

    /// <summary>Escenario de citas en el que el tinte exige prueba 48 h antes (el valor por defecto).</summary>
    private async Task<AppointmentScene> SceneRequiringTestAsync()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        await using var scope = factory.CreateTenantScope(TestData.OrgA);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tint = await db.Services.SingleAsync(s => s.Id == scene.Tint.Id);
        tint.RequiresAllergyTest = true;
        tint.AllergyTestHoursBefore = 48;
        await db.SaveChangesAsync();
        return scene;
    }

    private Task<ApiResult> BookTintAsync(string token, AppointmentScene scene, DateOnly? day = null) =>
        factory.SendAsync(HttpMethod.Post, "/api/v1/appointments", TestData.OrgA, token,
            scene.Tinting(new TimeOnly(10, 0)) with { AppointmentDate = day ?? AppointmentScene.Day });

    private Task<ApiResult> RecordAsync(string token, int customerId, string testedAt) =>
        factory.SendAsync(HttpMethod.Put, $"/api/v1/customers/{customerId}/allergy-test", TestData.OrgA, token,
            new { testedAt });

    private async Task<string> StaffTokenAsync(string rol)
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA, rol);
        return await factory.TokenForAsync(TestData.OrgA, employee.Id);
    }

    private static List<JsonElement> Warnings(JsonElement data) =>
        data.GetProperty("warnings").EnumerateArray().ToList();
}
