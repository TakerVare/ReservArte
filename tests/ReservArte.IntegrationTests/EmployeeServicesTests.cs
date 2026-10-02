using System.Net;
using AwesomeAssertions;
using ReservArte.Domain.Entities;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Servicios que presta cada empleado (4.1b): <c>GET|PUT /api/v1/employees/{id}/services</c>.
/// El PUT reemplaza el conjunto entero y decide quién sale en la reserva de cada servicio.
/// </summary>
[Collection(ApiCollection.Name)]
public class EmployeeServicesTests(ApiFactory factory)
{
    private const string Employees = "/api/v1/employees";
    private const string Availability = "/api/v1/appointments/availability";

    private static readonly DateOnly InWindow = DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid")))
        .AddDays(7);

    private async Task<string> TokenAsync(string rol)
    {
        var caller = await factory.CreateEmployeeAsync(TestData.OrgA, rol);
        return await factory.TokenForAsync(TestData.OrgA, caller.Id);
    }

    private Task<ApiResult> Put(string token, int employeeId, params int[] serviceIds) =>
        factory.SendAsync(
            HttpMethod.Put, $"{Employees}/{employeeId}/services", TestData.OrgA, token, new { serviceIds });

    [Fact]
    public async Task Asignar_servicios_los_devuelve_ordenados_y_el_empleado_sale_en_la_reserva()
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA);
        var service = await factory.CreateServiceAsync(TestData.OrgA, 45, 30m);
        var token = await TokenAsync(Roles.Manager);

        var before = await factory.SendAsync(
            HttpMethod.Get, $"{Availability}/by-service?serviceId={service.Id}&date={InWindow:yyyy-MM-dd}",
            TestData.OrgA, token);
        before.Data.GetProperty("employees").EnumerateArray()
            .Select(e => e.GetProperty("employeeId").GetInt32()).Should().NotContain(employee.Id);

        var result = await Put(token, employee.Id, service.Id, service.Id);

        result.Status.Should().Be(HttpStatusCode.OK);
        result.ShouldBeEnvelope(success: true);
        result.Data.GetProperty("employeeId").GetInt32().Should().Be(employee.Id);
        var services = result.Data.GetProperty("services").EnumerateArray().ToList();
        services.Should().ContainSingle();
        services[0].GetProperty("serviceId").GetInt32().Should().Be(service.Id);
        services[0].GetProperty("durationMinutes").GetInt32().Should().Be(45);
        services[0].GetProperty("proficiencyLevel").GetInt32().Should().Be(1);
        services[0].GetProperty("serviceIsActive").GetBoolean().Should().BeTrue();

        var after = await factory.SendAsync(
            HttpMethod.Get, $"{Availability}/by-service?serviceId={service.Id}&date={InWindow:yyyy-MM-dd}",
            TestData.OrgA, token);
        after.Data.GetProperty("employees").EnumerateArray()
            .Select(e => e.GetProperty("employeeId").GetInt32()).Should().Contain(employee.Id);

        // Una lista vacía lo deja sin servicios, y fuera de la reserva.
        (await Put(token, employee.Id)).Data.GetProperty("services").GetArrayLength().Should().Be(0);
        var get = await factory.SendAsync(HttpMethod.Get, $"{Employees}/{employee.Id}/services", TestData.OrgA, token);
        get.ShouldBeEnvelope(success: true);
        get.Data.GetProperty("services").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Un_servicio_retirado_o_inexistente_da_400_con_su_indice()
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA);
        var service = await factory.CreateServiceAsync(TestData.OrgA, 30, 20m);
        var token = await TokenAsync(Roles.Admin);

        var result = await Put(token, employee.Id, service.Id, 999_999);

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        var detail = result.Body.GetProperty("error").GetProperty("details").EnumerateArray().Single();
        detail.GetProperty("field").GetString().Should().Be("serviceIds[1]");
        detail.GetProperty("code").GetString().Should().Be("UnknownService");
    }

    [Fact]
    public async Task Un_servicio_de_otro_centro_no_se_puede_asignar_ni_se_ve_el_empleado_ajeno()
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA);
        var foreignService = await factory.CreateServiceAsync(TestData.OrgB, 30, 20m);
        var foreignEmployee = await factory.CreateEmployeeAsync(TestData.OrgB);
        var token = await TokenAsync(Roles.Admin);

        var assign = await Put(token, employee.Id, foreignService.Id);
        var read = await factory.SendAsync(
            HttpMethod.Get, $"{Employees}/{foreignEmployee.Id}/services", TestData.OrgA, token);

        assign.Status.Should().Be(HttpStatusCode.BadRequest);
        read.Status.Should().Be(HttpStatusCode.NotFound);
        read.ShouldBeEnvelope(success: false);
    }

    [Fact]
    public async Task Un_Manager_no_cambia_los_servicios_de_un_Admin_y_una_empleada_no_entra()
    {
        var admin = await factory.CreateEmployeeAsync(TestData.OrgA, Roles.Admin);
        var service = await factory.CreateServiceAsync(TestData.OrgA, 30, 20m);

        var byManager = await Put(await TokenAsync(Roles.Manager), admin.Id, service.Id);
        var byEmployee = await Put(await TokenAsync(Roles.Employee), admin.Id, service.Id);

        byManager.Status.Should().Be(HttpStatusCode.Forbidden);
        byManager.ShouldBeEnvelope(success: false);
        byEmployee.Status.Should().Be(HttpStatusCode.Forbidden);
        byEmployee.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
    }
}
