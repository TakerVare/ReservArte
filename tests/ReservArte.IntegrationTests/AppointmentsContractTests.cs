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
/// Contrato HTTP de <c>/api/v1/appointments</c> (RA-869d7f519): alta y edición por el
/// personal con fin, precio y duración calculados desde el catálogo, huecos, roles,
/// agenda por rol, baja lógica y las rutas de la máquina de estados. Cada test usa
/// su propia empleada (con horario de 09:00 a 18:00 todos los días) y sus servicios.
/// </summary>
[Collection(ApiCollection.Name)]
public class AppointmentsContractTests(ApiFactory factory)
{
    private const string Appointments = "/api/v1/appointments";
    private static readonly DateOnly Day = new(2031, 3, 3);

    // ── Alta ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task El_personal_crea_una_cita_pending_con_fin_precio_y_lineas_calculados()
    {
        var scene = await SceneAsync();
        var (staff, token) = await StaffAsync(Roles.Employee);

        var result = await Post(token, NewAppointment(scene, new TimeOnly(10, 0),
            (scene.Brows.Id, scene.BrowsThread), (scene.Tint.Id, null)));

        result.Status.Should().Be(HttpStatusCode.Created);
        result.ShouldBeEnvelope(success: true);
        var data = result.Data;
        result.Headers.Location!.AbsolutePath.Should().Be($"{Appointments}/{data.GetProperty("id").GetInt32()}");
        data.GetProperty("status").GetString().Should().Be(AppointmentStatuses.Pending);
        // Cejas 45 min + hilo 15 min; tinte 30 min → 90 min; 25 + 5 + 20 = 50 €.
        data.GetProperty("endTime").GetString().Should().Be("11:30:00");
        data.GetProperty("totalPrice").GetDecimal().Should().Be(50m);
        data.GetProperty("createdById").GetInt32().Should().Be(staff.Id);
        data.GetProperty("customerName").GetString().Should().NotBeNullOrEmpty();
        var items = data.GetProperty("items").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("order").GetInt32()).Should().Equal(1, 2);
        items[0].GetProperty("serviceVariationName").GetString().Should().Be("Con hilo");
        items[0].GetProperty("durationMinutes").GetInt32().Should().Be(60);
        items[0].GetProperty("price").GetDecimal().Should().Be(30m);
    }

    [Fact]
    public async Task Un_hueco_ocupado_o_fuera_de_horario_da_409_APT_SLOT_UNAVAILABLE()
    {
        var scene = await SceneAsync();
        var (_, token) = await StaffAsync(Roles.Employee);
        (await Post(token, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null))))
            .Status.Should().Be(HttpStatusCode.Created);

        var overlapping = await Post(token, NewAppointment(scene, new TimeOnly(10, 15), (scene.Tint.Id, null)));
        var outsideSchedule = await Post(token, NewAppointment(scene, new TimeOnly(17, 45), (scene.Tint.Id, null)));

        foreach (var result in new[] { overlapping, outsideSchedule })
        {
            result.Status.Should().Be(HttpStatusCode.Conflict);
            result.ShouldBeEnvelope(success: false);
            result.ErrorCode.Should().Be(ErrorCodes.AptSlotUnavailable);
        }
    }

    [Fact]
    public async Task Un_servicio_que_la_empleada_no_presta_da_400_EmployeeNotQualified()
    {
        var scene = await SceneAsync();
        var notAssigned = await factory.CreateServiceAsync(TestData.OrgA, 30, 15m);
        var (_, token) = await StaffAsync(Roles.Employee);

        var result = await Post(token, NewAppointment(scene, new TimeOnly(10, 0), (notAssigned.Id, null)));

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        var detail = Details(result).Single();
        detail.GetProperty("field").GetString().Should().Be("items[0].serviceId");
        detail.GetProperty("code").GetString().Should().Be("EmployeeNotQualified");
    }

    [Fact]
    public async Task Una_variacion_de_otro_servicio_da_400_en_su_campo()
    {
        var scene = await SceneAsync();
        var (_, token) = await StaffAsync(Roles.Employee);

        var result = await Post(token, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, scene.BrowsThread)));

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        Details(result).Single().GetProperty("field").GetString().Should().Be("items[0].serviceVariationId");
    }

    [Fact]
    public async Task Una_cita_que_acabaria_a_medianoche_o_despues_da_400_en_startTime()
    {
        var scene = await SceneAsync();
        var (_, token) = await StaffAsync(Roles.Employee);

        var result = await Post(token, NewAppointment(scene, new TimeOnly(23, 30), (scene.Tint.Id, null)));

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        Details(result).Single().GetProperty("field").GetString().Should().Be("startTime");
    }

    [Fact]
    public async Task Una_clienta_bloqueada_no_puede_reservar_403_CUST_BLOCKED()
    {
        var scene = await SceneAsync();
        await using (var scope = factory.CreateTenantScope(TestData.OrgA))
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customer = await db.Customers.SingleAsync(c => c.Id == scene.Customer.Id);
            customer.IsBlocked = true;
            customer.BlockedReason = "Impagos";
            await db.SaveChangesAsync();
        }

        var (_, token) = await StaffAsync(Roles.Employee);
        var result = await Post(token, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null)));

        result.Status.Should().Be(HttpStatusCode.Forbidden);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.CustBlocked);
    }

    [Fact]
    public async Task El_personal_puede_registrar_una_cita_en_el_pasado()
    {
        var scene = await SceneAsync();
        var (_, token) = await StaffAsync(Roles.Employee);

        var body = NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null)) with { AppointmentDate = new DateOnly(2025, 1, 15) };

        var result = await Post(token, body);

        result.Status.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Una_clienta_no_puede_crear_citas()
    {
        var scene = await SceneAsync();
        var token = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);

        var result = await Post(token, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null)));

        result.Status.Should().Be(HttpStatusCode.Forbidden);
        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
    }

    [Fact]
    public async Task Un_alta_invalida_da_400_con_los_campos_en_camelCase()
    {
        var (_, token) = await StaffAsync(Roles.Employee);

        var result = await factory.SendAsync(HttpMethod.Post, Appointments, TestData.OrgA, token,
            new { customerId = 0, employeeId = 0, appointmentDate = "2031-03-03", startTime = "10:00:00", items = Array.Empty<object>() });

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        Details(result).Select(d => d.GetProperty("field").GetString())
            .Should().Contain(["customerId", "employeeId", "items"]);
    }

    // ── Lectura por rol ───────────────────────────────────────────────────

    [Fact]
    public async Task La_clienta_ve_solo_sus_citas_y_la_ajena_le_da_404()
    {
        var scene = await SceneAsync();
        var other = await factory.CreateCustomerAsync(TestData.OrgA);
        var (_, staffToken) = await StaffAsync(Roles.Employee);
        var own = await Post(staffToken, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null)));
        var foreignBody = NewAppointment(scene, new TimeOnly(12, 0), (scene.Tint.Id, null)) with { CustomerId = other.Id };
        var foreign = await Post(staffToken, foreignBody);
        var customerToken = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);

        var list = await factory.SendAsync(HttpMethod.Get,
            $"{Appointments}?customerId={other.Id}&pageSize=100", TestData.OrgA, customerToken);
        var ownDetail = await factory.SendAsync(HttpMethod.Get, $"{Appointments}/{Id(own)}", TestData.OrgA, customerToken);
        var foreignDetail = await factory.SendAsync(HttpMethod.Get, $"{Appointments}/{Id(foreign)}", TestData.OrgA, customerToken);

        list.Status.Should().Be(HttpStatusCode.OK);
        list.Data.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("customerId").GetInt32())
            .Should().OnlyContain(id => id == scene.Customer.Id).And.NotBeEmpty();
        ownDetail.Status.Should().Be(HttpStatusCode.OK);
        foreignDetail.Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task La_agenda_del_personal_filtra_por_empleada_y_fechas()
    {
        var scene = await SceneAsync();
        var (_, token) = await StaffAsync(Roles.Employee);
        await Post(token, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null)));
        var nextDay = NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null)) with { AppointmentDate = Day.AddDays(1) };
        await Post(token, nextDay);

        var result = await factory.SendAsync(HttpMethod.Get,
            $"{Appointments}?employeeId={scene.Employee.Id}&from={Day:yyyy-MM-dd}&to={Day:yyyy-MM-dd}",
            TestData.OrgA, token);

        result.Status.Should().Be(HttpStatusCode.OK);
        result.Body.GetProperty("meta").GetProperty("pagination").GetProperty("totalCount").GetInt32().Should().Be(1);
        var item = result.Data.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("employeeName").GetString().Should().NotBeNullOrEmpty();
        item.GetProperty("appointmentDate").GetString().Should().Be("2031-03-03");
    }

    // ── Edición y baja ────────────────────────────────────────────────────

    [Fact]
    public async Task Reagendar_recalcula_el_fin_y_no_choca_consigo_misma()
    {
        var scene = await SceneAsync();
        var (_, token) = await StaffAsync(Roles.Employee);
        var created = await Post(token, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null)));

        var result = await factory.SendAsync(HttpMethod.Put, $"{Appointments}/{Id(created)}", TestData.OrgA, token,
            new
            {
                employeeId = scene.Employee.Id,
                appointmentDate = "2031-03-03",
                startTime = "10:15:00",
                items = new[] { new { serviceId = scene.Brows.Id, serviceVariationId = (int?)null } },
                notes = "Movida un cuarto de hora",
            });

        result.Status.Should().Be(HttpStatusCode.OK);
        result.Data.GetProperty("startTime").GetString().Should().Be("10:15:00");
        result.Data.GetProperty("endTime").GetString().Should().Be("11:00:00");
        result.Data.GetProperty("totalPrice").GetDecimal().Should().Be(25m);
        result.Data.GetProperty("items").EnumerateArray().Single().GetProperty("serviceId").GetInt32()
            .Should().Be(scene.Brows.Id);
    }

    [Fact]
    public async Task Una_cita_ya_empezada_no_se_puede_editar_409_APT_INVALID_STATE()
    {
        var scene = await SceneAsync();
        var (_, token) = await StaffAsync(Roles.Employee);
        var id = Id(await Post(token, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null))));
        await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/confirm", TestData.OrgA, token);
        await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/start", TestData.OrgA, token);

        var result = await factory.SendAsync(HttpMethod.Put, $"{Appointments}/{id}", TestData.OrgA, token,
            new
            {
                employeeId = scene.Employee.Id,
                appointmentDate = "2031-03-03",
                startTime = "11:00:00",
                items = new[] { new { serviceId = scene.Tint.Id } },
            });

        result.Status.Should().Be(HttpStatusCode.Conflict);
        result.ErrorCode.Should().Be(ErrorCodes.AptInvalidState);
    }

    [Fact]
    public async Task La_baja_es_de_la_gerencia_y_saca_la_cita_de_la_agenda()
    {
        var scene = await SceneAsync();
        var (_, employee) = await StaffAsync(Roles.Employee);
        var (_, manager) = await StaffAsync(Roles.Manager);
        var id = Id(await Post(employee, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null))));

        var byEmployee = await factory.SendAsync(HttpMethod.Delete, $"{Appointments}/{id}", TestData.OrgA, employee);
        var byManager = await factory.SendAsync(HttpMethod.Delete, $"{Appointments}/{id}", TestData.OrgA, manager);
        var agenda = await factory.SendAsync(HttpMethod.Get,
            $"{Appointments}?employeeId={scene.Employee.Id}", TestData.OrgA, manager);

        byEmployee.Status.Should().Be(HttpStatusCode.Forbidden);
        byManager.Status.Should().Be(HttpStatusCode.OK);
        byManager.Data.GetProperty("isActive").GetBoolean().Should().BeFalse();
        agenda.Data.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // ── Transiciones por HTTP ─────────────────────────────────────────────

    [Fact]
    public async Task Confirmar_empezar_y_completar_por_HTTP()
    {
        var scene = await SceneAsync();
        var (_, token) = await StaffAsync(Roles.Employee);
        var id = Id(await Post(token, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null))));

        var statuses = new List<string?>();
        foreach (var step in new[] { "confirm", "start", "complete" })
        {
            var result = await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/{step}", TestData.OrgA, token);
            result.Status.Should().Be(HttpStatusCode.OK);
            statuses.Add(result.Data.GetProperty("status").GetString());
        }

        statuses.Should().Equal(AppointmentStatuses.Confirmed, AppointmentStatuses.InProgress, AppointmentStatuses.Completed);
        var again = await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/confirm", TestData.OrgA, token);
        again.Status.Should().Be(HttpStatusCode.Conflict);
        again.ErrorCode.Should().Be(ErrorCodes.AptInvalidState);
    }

    [Fact]
    public async Task La_clienta_cancela_la_suya_sin_cuerpo_y_una_empleada_no_marca_no_show()
    {
        var scene = await SceneAsync();
        var (_, staff) = await StaffAsync(Roles.Employee);
        var first = Id(await Post(staff, NewAppointment(scene, new TimeOnly(10, 0), (scene.Tint.Id, null))));
        var second = Id(await Post(staff, NewAppointment(scene, new TimeOnly(12, 0), (scene.Tint.Id, null))));
        var customer = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);

        var cancel = await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{first}/cancel", TestData.OrgA, customer);
        var noShow = await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{second}/no-show", TestData.OrgA, staff);

        cancel.Status.Should().Be(HttpStatusCode.OK);
        cancel.Data.GetProperty("status").GetString().Should().Be(AppointmentStatuses.CancelledByCustomer);
        noShow.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Escenario y ayudantes ─────────────────────────────────────────────

    /// <summary>Empleada con horario, clienta y dos servicios asignados (cejas con variación «Con hilo» y tinte).</summary>
    private sealed record Scene(Employee Employee, Customer Customer, Service Brows, int BrowsThread, Service Tint);

    private async Task<Scene> SceneAsync()
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA);
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var brows = await factory.CreateServiceAsync(TestData.OrgA, 45, 25m, [employee], ("Con hilo", 5m, 15));
        var tint = await factory.CreateServiceAsync(TestData.OrgA, 30, 20m, [employee]);
        return new Scene(employee, customer, brows, brows.Variations.Single().Id, tint);
    }

    private sealed record NewAppointmentBody(
        int CustomerId, int EmployeeId, DateOnly AppointmentDate, TimeOnly StartTime, object[] Items, string? Notes);

    private static NewAppointmentBody NewAppointment(
        Scene scene, TimeOnly start, params (int ServiceId, int? VariationId)[] items) =>
        new(scene.Customer.Id, scene.Employee.Id, Day, start,
            items.Select(i => (object)new { serviceId = i.ServiceId, serviceVariationId = i.VariationId }).ToArray(),
            null);

    private async Task<(Employee Employee, string Token)> StaffAsync(string rol)
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA, rol);
        return (employee, await factory.TokenForAsync(TestData.OrgA, employee.Id));
    }

    private Task<ApiResult> Post(string token, NewAppointmentBody body) =>
        factory.SendAsync(HttpMethod.Post, Appointments, TestData.OrgA, token, body);

    private static int Id(ApiResult result) => result.Data.GetProperty("id").GetInt32();

    private static List<JsonElement> Details(ApiResult result) =>
        result.Body.GetProperty("error").GetProperty("details").EnumerateArray().ToList();
}
