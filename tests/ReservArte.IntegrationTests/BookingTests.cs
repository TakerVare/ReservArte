using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using ReservArte.Domain.Entities;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Pantalla de reserva (RA-869fagpx9, H-44 y H-45): huecos por servicio agrupados por
/// empleado, días con hueco para el calendario, ventana de reserva por rol (6 semanas
/// la clienta, 10 el personal) y reserva y modificación por la propia clienta con una
/// sola cita activa. Las fechas dependen del reloj real: se calculan desde hoy en la
/// hora del centro.
/// </summary>
[Collection(ApiCollection.Name)]
public class BookingTests(ApiFactory factory)
{
    private const string Appointments = "/api/v1/appointments";
    private const string Availability = "/api/v1/appointments/availability";

    private static readonly DateOnly Today = DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid")));

    /// <summary>Dentro de la ventana de la clienta (6 semanas = 42 días).</summary>
    private static readonly DateOnly InWindow = Today.AddDays(7);

    // ── Huecos por servicio ───────────────────────────────────────────────

    [Fact]
    public async Task Los_huecos_de_un_servicio_salen_agrupados_por_los_empleados_que_lo_prestan()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var other = await factory.CreateEmployeeAsync(TestData.OrgA);
        var notQualified = await factory.CreateEmployeeAsync(TestData.OrgA);
        var service = await factory.CreateServiceAsync(TestData.OrgA, 45, 30m, [scene.Employee, other]);
        _ = notQualified;
        var token = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);

        var result = await Get(token, $"{Availability}/by-service?serviceId={service.Id}&date={InWindow:yyyy-MM-dd}");

        result.Status.Should().Be(HttpStatusCode.OK);
        result.ShouldBeEnvelope(success: true);
        result.Data.GetProperty("durationMinutes").GetInt32().Should().Be(45);
        result.Data.GetProperty("bookableUntil").GetString().Should().Be(Today.AddDays(42).ToString("yyyy-MM-dd"));
        var employees = result.Data.GetProperty("employees").EnumerateArray().ToList();
        employees.Select(e => e.GetProperty("employeeId").GetInt32())
            .Should().BeEquivalentTo([scene.Employee.Id, other.Id]);
        var slots = employees[0].GetProperty("slots").EnumerateArray().ToList();
        slots[0].GetProperty("startTime").GetString().Should().Be("09:00:00");
        slots[0].GetProperty("endTime").GetString().Should().Be("09:45:00");
    }

    [Fact]
    public async Task Un_empleado_sin_huecos_ese_dia_no_aparece()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var service = await factory.CreateServiceAsync(TestData.OrgA, 9 * 60, 30m, [scene.Employee]);
        var staff = await factory.CreateEmployeeAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, staff.Id);
        // Una cita de todo el día deja a la empleada sin huecos.
        (await Send(token, HttpMethod.Post, Appointments, new
        {
            customerId = scene.Customer.Id,
            employeeId = scene.Employee.Id,
            appointmentDate = InWindow,
            startTime = "09:00",
            items = new[] { new { serviceId = service.Id } },
        })).Status.Should().Be(HttpStatusCode.Created);

        var result = await Get(token, $"{Availability}/by-service?serviceId={service.Id}&date={InWindow:yyyy-MM-dd}");

        result.Data.GetProperty("employees").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task La_ventana_depende_del_rol_6_semanas_la_clienta_y_10_el_personal()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var customerToken = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);
        var staff = await factory.CreateEmployeeAsync(TestData.OrgA);
        var staffToken = await factory.TokenForAsync(TestData.OrgA, staff.Id);
        var day = Today.AddDays(50);
        var path = $"{Availability}/by-service?serviceId={scene.Tint.Id}&date={day:yyyy-MM-dd}";

        var asCustomer = await Get(customerToken, path);
        var asStaff = await Get(staffToken, path);

        asCustomer.Data.GetProperty("employees").GetArrayLength().Should().Be(0);
        asStaff.Data.GetProperty("employees").GetArrayLength().Should().Be(1);
        asStaff.Data.GetProperty("bookableUntil").GetString().Should().Be(Today.AddDays(70).ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task Un_servicio_inexistente_da_404_y_sin_parametros_400_con_envelope()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);

        var missing = await Get(token, $"{Availability}/by-service");
        var unknown = await Get(token, $"{Availability}/by-service?serviceId=999999&date={InWindow:yyyy-MM-dd}");

        missing.Status.Should().Be(HttpStatusCode.BadRequest);
        missing.ShouldBeEnvelope(success: false);
        Fields(missing).Should().BeEquivalentTo(["serviceId", "date"]);
        unknown.Status.Should().Be(HttpStatusCode.NotFound);
        unknown.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    // ── Días con hueco ────────────────────────────────────────────────────

    [Fact]
    public async Task Los_dias_con_hueco_se_recortan_a_la_ventana_y_no_incluyen_el_pasado()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);
        var from = Today.AddDays(-5);
        var to = Today.AddDays(55);

        var result = await Get(token, $"{Availability}/days?serviceId={scene.Tint.Id}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

        result.Status.Should().Be(HttpStatusCode.OK);
        var days = result.Data.GetProperty("days").EnumerateArray().Select(d => DateOnly.Parse(d.GetString()!)).ToList();
        days.Should().NotBeEmpty();
        days.Should().OnlyContain(d => d >= Today && d <= Today.AddDays(42));
        // La empleada trabaja todos los días: todos los de mañana en adelante tienen hueco.
        days.Should().Contain(Today.AddDays(1)).And.Contain(Today.AddDays(42));
    }

    [Fact]
    public async Task Un_intervalo_al_reves_o_de_mas_de_62_dias_da_400()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);

        var reversed = await Get(token, $"{Availability}/days?serviceId={scene.Tint.Id}&from={Today.AddDays(5):yyyy-MM-dd}&to={Today:yyyy-MM-dd}");
        var tooLong = await Get(token, $"{Availability}/days?serviceId={scene.Tint.Id}&from={Today:yyyy-MM-dd}&to={Today.AddDays(62):yyyy-MM-dd}");

        reversed.Status.Should().Be(HttpStatusCode.BadRequest);
        tooLong.Status.Should().Be(HttpStatusCode.BadRequest);
        Fields(tooLong).Should().Equal("to");
    }

    // ── Reserva por la clienta (H-44) ─────────────────────────────────────

    [Fact]
    public async Task La_clienta_reserva_para_si_misma_aunque_el_cuerpo_diga_otra_y_sin_notas()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var otherCustomer = await factory.CreateCustomerAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);

        var result = await Send(token, HttpMethod.Post, Appointments, new
        {
            customerId = otherCustomer.Id,
            employeeId = scene.Employee.Id,
            appointmentDate = InWindow,
            startTime = "10:00",
            items = new[] { new { serviceId = scene.Tint.Id } },
            notes = "nota",
        });

        result.Status.Should().Be(HttpStatusCode.Created);
        result.Data.GetProperty("customerId").GetInt32().Should().Be(scene.Customer.Id);
        result.Data.GetProperty("createdById").GetInt32().Should().Be(scene.Customer.Id);
        result.Data.GetProperty("notes").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Con_una_cita_activa_la_clienta_no_reserva_otra_409_APT_ACTIVE_EXISTS_y_la_modifica()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);
        var first = await Book(token, scene, InWindow, "10:00");
        first.Status.Should().Be(HttpStatusCode.Created);

        var second = await Book(token, scene, InWindow.AddDays(1), "11:00");
        var moved = await Send(token, HttpMethod.Put, $"{Appointments}/{first.Data.GetProperty("id").GetInt32()}", new
        {
            employeeId = scene.Employee.Id,
            appointmentDate = InWindow.AddDays(1),
            startTime = "11:00",
            items = new[] { new { serviceId = scene.Tint.Id } },
        });

        second.Status.Should().Be(HttpStatusCode.Conflict);
        second.ShouldBeEnvelope(success: false);
        second.ErrorCode.Should().Be(ErrorCodes.AptActiveExists);
        moved.Status.Should().Be(HttpStatusCode.OK);
        moved.Data.GetProperty("appointmentDate").GetString().Should().Be(InWindow.AddDays(1).ToString("yyyy-MM-dd"));
        moved.Data.GetProperty("startTime").GetString().Should().Be("11:00:00");
    }

    [Fact]
    public async Task Fuera_de_su_ventana_la_clienta_recibe_400_OutsideBookingWindow()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);

        var tooFar = await Book(token, scene, Today.AddDays(43), "10:00");
        var past = await Book(token, scene, Today.AddDays(-1), "10:00");

        foreach (var result in new[] { tooFar, past })
        {
            result.Status.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
            var detail = result.Body.GetProperty("error").GetProperty("details")[0];
            detail.GetProperty("field").GetString().Should().Be("appointmentDate");
            detail.GetProperty("code").GetString().Should().Be("OutsideBookingWindow");
        }
    }

    [Fact]
    public async Task La_clienta_no_puede_modificar_una_cita_ajena_404()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var other = await factory.CreateCustomerAsync(TestData.OrgA);
        var ownerToken = await factory.TokenForAsync(TestData.OrgA, scene.Customer.Id);
        var otherToken = await factory.TokenForAsync(TestData.OrgA, other.Id);
        var booked = await Book(ownerToken, scene, InWindow, "10:00");

        var result = await Send(otherToken, HttpMethod.Put, $"{Appointments}/{booked.Data.GetProperty("id").GetInt32()}", new
        {
            employeeId = scene.Employee.Id,
            appointmentDate = InWindow,
            startTime = "12:00",
            items = new[] { new { serviceId = scene.Tint.Id } },
        });

        result.Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task El_personal_sigue_teniendo_que_indicar_la_clienta()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var staff = await factory.CreateEmployeeAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, staff.Id);

        var result = await Send(token, HttpMethod.Post, Appointments, new
        {
            employeeId = scene.Employee.Id,
            appointmentDate = InWindow,
            startTime = "10:00",
            items = new[] { new { serviceId = scene.Tint.Id } },
        });

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        Fields(result).Should().Equal("customerId");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Task<ApiResult> Book(string token, AppointmentScene scene, DateOnly date, string start) =>
        Send(token, HttpMethod.Post, Appointments, new
        {
            employeeId = scene.Employee.Id,
            appointmentDate = date,
            startTime = start,
            items = new[] { new { serviceId = scene.Tint.Id } },
        });

    private Task<ApiResult> Get(string token, string path) =>
        factory.SendAsync(HttpMethod.Get, path, TestData.OrgA, token);

    private Task<ApiResult> Send(string token, HttpMethod method, string path, object body) =>
        factory.SendAsync(method, path, TestData.OrgA, token, body);

    private static List<string?> Fields(ApiResult result) =>
        result.Body.GetProperty("error").GetProperty("details").EnumerateArray()
            .Select(d => d.GetProperty("field").GetString()).ToList();
}
