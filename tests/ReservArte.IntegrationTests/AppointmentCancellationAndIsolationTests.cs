using System.Net;
using AwesomeAssertions;
using ReservArte.Domain.Entities;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Cancelación y aislamiento de citas de punta a punta, por HTTP y contra PostgreSQL
/// (RA-869d7f53r, sin penalización: eso es de RA-869f7axdq). Las reglas de la máquina
/// de estados ya las prueban los tests unitarios con dobles; aquí se comprueba lo que
/// solo se ve con la API entera: que cancelar libera el hueco, que lo que se guarda
/// es lo que se devuelve y que ninguna ruta de citas deja tocar otro centro.
/// </summary>
[Collection(ApiCollection.Name)]
public class AppointmentCancellationAndIsolationTests(ApiFactory factory)
{
    private const string Appointments = "/api/v1/appointments";

    // ── Cancelación ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("cancel")]
    [InlineData("no-show")]
    public async Task Cancelar_o_marcar_no_show_libera_el_hueco_para_otra_reserva(string action)
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var manager = await StaffTokenAsync(Roles.Manager);
        var first = Id(await Post(manager, scene.Tinting(new TimeOnly(10, 0))));
        (await Post(manager, scene.Tinting(new TimeOnly(10, 0)))).Status.Should().Be(HttpStatusCode.Conflict);

        (await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{first}/{action}", TestData.OrgA, manager))
            .Status.Should().Be(HttpStatusCode.OK);
        var rebooked = await Post(manager, scene.Tinting(new TimeOnly(10, 0)));
        var slots = await factory.SendAsync(HttpMethod.Get,
            $"/api/v1/appointments/availability?employeeId={scene.Employee.Id}&date={AppointmentScene.Day:yyyy-MM-dd}&durationMinutes=30",
            TestData.OrgA, manager);

        rebooked.Status.Should().Be(HttpStatusCode.Created);
        // Tras la nueva reserva de 10:00 a 10:30, el hueco vuelve a estar ocupado.
        slots.Data.GetProperty("slots").EnumerateArray().Select(s => s.GetProperty("startTime").GetString())
            .Should().NotContain("10:00:00").And.Contain("10:30:00");
    }

    [Fact]
    public async Task La_cancelacion_guarda_y_devuelve_motivo_fecha_autor_y_tipo_y_la_cita_sigue_en_la_agenda()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var staff = await factory.CreateEmployeeAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, staff.Id);
        var id = Id(await Post(token, scene.Tinting(new TimeOnly(10, 0))));

        var cancelled = await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/cancel", TestData.OrgA, token,
            new { reason = "  La clienta avisa por teléfono  " });
        var detail = await factory.SendAsync(HttpMethod.Get, $"{Appointments}/{id}", TestData.OrgA, token);
        var byStatus = await factory.SendAsync(HttpMethod.Get,
            $"{Appointments}?employeeId={scene.Employee.Id}&status={AppointmentStatuses.CancelledByBusiness}",
            TestData.OrgA, token);

        cancelled.Status.Should().Be(HttpStatusCode.OK);
        foreach (var data in new[] { cancelled.Data, detail.Data })
        {
            data.GetProperty("status").GetString().Should().Be(AppointmentStatuses.CancelledByBusiness);
            data.GetProperty("cancelledByType").GetString().Should().Be(AppointmentCancelledByTypes.Business);
            data.GetProperty("cancelledById").GetInt32().Should().Be(staff.Id);
            data.GetProperty("cancellationReason").GetString().Should().Be("La clienta avisa por teléfono");
            data.GetProperty("cancelledAt").GetString().Should().EndWith("Z");
            data.GetProperty("isActive").GetBoolean().Should().BeTrue();
        }

        byStatus.Data.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32())
            .Should().Equal(id);
    }

    [Fact]
    public async Task Una_cita_cancelada_no_se_cancela_otra_vez_ni_se_edita()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await StaffTokenAsync(Roles.Employee);
        var id = Id(await Post(token, scene.Tinting(new TimeOnly(10, 0))));
        await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/cancel", TestData.OrgA, token);

        var again = await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/cancel", TestData.OrgA, token);
        var edit = await factory.SendAsync(HttpMethod.Put, $"{Appointments}/{id}", TestData.OrgA, token,
            new
            {
                employeeId = scene.Employee.Id,
                appointmentDate = "2031-03-03",
                startTime = "11:00:00",
                items = new[] { new { serviceId = scene.Tint.Id } },
            });

        foreach (var result in new[] { again, edit })
        {
            result.Status.Should().Be(HttpStatusCode.Conflict);
            result.ShouldBeEnvelope(success: false);
            result.ErrorCode.Should().Be(ErrorCodes.AptInvalidState);
        }
    }

    [Fact]
    public async Task Un_motivo_de_mas_de_500_caracteres_da_400_en_reason()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await StaffTokenAsync(Roles.Employee);
        var id = Id(await Post(token, scene.Tinting(new TimeOnly(10, 0))));

        var result = await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/cancel", TestData.OrgA, token,
            new { reason = new string('x', 501) });

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.Body.GetProperty("error").GetProperty("details").EnumerateArray()
            .Select(d => d.GetProperty("field").GetString()).Should().Contain("reason");
    }

    [Fact]
    public async Task Una_clienta_no_cancela_la_cita_de_otra_clienta_del_mismo_centro()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var other = await factory.CreateCustomerAsync(TestData.OrgA);
        var staff = await StaffTokenAsync(Roles.Employee);
        var id = Id(await Post(staff, scene.Tinting(new TimeOnly(10, 0))));
        var otherToken = await factory.TokenForAsync(TestData.OrgA, other.Id);

        var result = await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/cancel", TestData.OrgA, otherToken);
        var detail = await factory.SendAsync(HttpMethod.Get, $"{Appointments}/{id}", TestData.OrgA, staff);

        result.Status.Should().Be(HttpStatusCode.NotFound);
        detail.Data.GetProperty("status").GetString().Should().Be(AppointmentStatuses.Pending);
    }

    // ── Aislamiento entre centros ─────────────────────────────────────────

    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "")]
    [InlineData("DELETE", "")]
    [InlineData("POST", "/confirm")]
    [InlineData("POST", "/start")]
    [InlineData("POST", "/complete")]
    [InlineData("POST", "/no-show")]
    [InlineData("POST", "/cancel")]
    public async Task Ninguna_ruta_toca_una_cita_de_otro_centro(string method, string suffix)
    {
        var sceneB = await factory.CreateAppointmentSceneAsync(TestData.OrgB);
        var appointmentB = await factory.CreateAppointmentAsync(
            TestData.OrgB, sceneB.Employee, sceneB.Customer, AppointmentScene.Day, new TimeOnly(10, 0), new TimeOnly(10, 30));
        var adminA = await StaffTokenAsync(Roles.Admin);
        object? body = method == "PUT"
            ? new
            {
                employeeId = sceneB.Employee.Id,
                appointmentDate = "2031-03-03",
                startTime = "11:00:00",
                items = new[] { new { serviceId = sceneB.Tint.Id } },
            }
            : null;

        var result = await factory.SendAsync(
            new HttpMethod(method), $"{Appointments}/{appointmentB.Id}{suffix}", TestData.OrgA, adminA, body);

        result.Status.Should().Be(HttpStatusCode.NotFound);
        result.ShouldBeEnvelope(success: false);
        var unchanged = await factory.SendAsync(HttpMethod.Get, $"{Appointments}/{appointmentB.Id}", TestData.OrgB,
            await StaffTokenAsync(Roles.Admin, TestData.OrgB));
        unchanged.Data.GetProperty("status").GetString().Should().Be(AppointmentStatuses.Pending);
        unchanged.Data.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task La_agenda_de_un_centro_no_trae_citas_de_otro_aunque_se_filtre_por_su_empleada()
    {
        var sceneB = await factory.CreateAppointmentSceneAsync(TestData.OrgB);
        await factory.CreateAppointmentAsync(
            TestData.OrgB, sceneB.Employee, sceneB.Customer, AppointmentScene.Day, new TimeOnly(10, 0), new TimeOnly(10, 30));
        var adminA = await StaffTokenAsync(Roles.Admin);

        var result = await factory.SendAsync(HttpMethod.Get,
            $"{Appointments}?employeeId={sceneB.Employee.Id}&customerId={sceneB.Customer.Id}", TestData.OrgA, adminA);

        result.Status.Should().Be(HttpStatusCode.OK);
        result.Data.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData("customer", "customerId")]
    [InlineData("employee", "employeeId")]
    [InlineData("service", "items[0].serviceId")]
    public async Task No_se_reserva_con_la_clienta_la_empleada_o_un_servicio_de_otro_centro(string foreign, string field)
    {
        var sceneA = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var sceneB = await factory.CreateAppointmentSceneAsync(TestData.OrgB);
        var body = sceneA.Tinting(new TimeOnly(10, 0));
        body = foreign switch
        {
            "customer" => body with { CustomerId = sceneB.Customer.Id },
            "employee" => body with { EmployeeId = sceneB.Employee.Id },
            _ => body with { Items = [new { serviceId = sceneB.Tint.Id, serviceVariationId = (int?)null }] },
        };

        var result = await Post(await StaffTokenAsync(Roles.Admin), body);

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.Body.GetProperty("error").GetProperty("details").EnumerateArray()
            .Select(d => d.GetProperty("field").GetString()).Should().Equal(field);
    }

    [Fact]
    public async Task La_disponibilidad_de_una_empleada_de_otro_centro_da_404()
    {
        var sceneB = await factory.CreateAppointmentSceneAsync(TestData.OrgB);
        var adminA = await StaffTokenAsync(Roles.Admin);

        var result = await factory.SendAsync(HttpMethod.Get,
            $"/api/v1/appointments/availability?employeeId={sceneB.Employee.Id}&date={AppointmentScene.Day:yyyy-MM-dd}&durationMinutes=30",
            TestData.OrgA, adminA);

        result.Status.Should().Be(HttpStatusCode.NotFound);
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    // ── Ayudantes ─────────────────────────────────────────────────────────

    private async Task<string> StaffTokenAsync(string rol, Guid? organizationId = null)
    {
        var org = organizationId ?? TestData.OrgA;
        var employee = await factory.CreateEmployeeAsync(org, rol);
        return await factory.TokenForAsync(org, employee.Id);
    }

    private Task<ApiResult> Post(string token, AppointmentBody body) =>
        factory.SendAsync(HttpMethod.Post, Appointments, TestData.OrgA, token, body);

    private static int Id(ApiResult result) => result.Data.GetProperty("id").GetInt32();
}
