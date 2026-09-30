using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Historial de citas de la clienta, <c>GET /api/v1/customers/{id}/history</c>
/// (RA-869f2gn91): solo el personal, citas activas en cualquier estado, de la más
/// reciente a la más antigua y con sus líneas; clienta ajena o inexistente → 404.
/// </summary>
[Collection(ApiCollection.Name)]
public class CustomerHistoryTests(ApiFactory factory)
{
    [Fact]
    public async Task El_historial_trae_las_citas_activas_de_la_clienta_de_la_mas_reciente_a_la_mas_antigua()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await StaffTokenAsync(Roles.Employee);
        var oldest = await BookAsync(token, scene, AppointmentScene.Day.AddDays(-7), new TimeOnly(10, 0));
        var cancelled = await BookAsync(token, scene, AppointmentScene.Day, new TimeOnly(9, 0));
        var newest = await BookAsync(token, scene, AppointmentScene.Day, new TimeOnly(16, 0));
        var retired = await BookAsync(token, scene, AppointmentScene.Day.AddDays(1), new TimeOnly(10, 0));
        await factory.SendAsync(HttpMethod.Post, $"/api/v1/appointments/{cancelled}/cancel", TestData.OrgA, token);
        await factory.SendAsync(HttpMethod.Delete, $"/api/v1/appointments/{retired}", TestData.OrgA,
            await StaffTokenAsync(Roles.Manager));
        // Otra clienta con la misma empleada: no debe colarse.
        var other = await factory.CreateCustomerAsync(TestData.OrgA);
        await factory.SendAsync(HttpMethod.Post, "/api/v1/appointments", TestData.OrgA, token,
            scene.Tinting(new TimeOnly(12, 0)) with { CustomerId = other.Id });

        var result = await HistoryAsync(token, scene.Customer.Id);

        result.Status.Should().Be(HttpStatusCode.OK);
        result.ShouldBeEnvelope(success: true);
        var items = result.Data.GetProperty("items").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("id").GetInt32()).Should().Equal(newest, cancelled, oldest);
        items.Select(i => i.GetProperty("status").GetString())
            .Should().Contain(AppointmentStatuses.CancelledByBusiness);
        items.Should().OnlyContain(i => i.GetProperty("items").GetArrayLength() == 1);
        items[0].GetProperty("items")[0].GetProperty("serviceName").GetString().Should().NotBeNullOrEmpty();
        result.Body.GetProperty("meta").GetProperty("pagination").GetProperty("totalCount").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task El_historial_se_pagina()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await StaffTokenAsync(Roles.Employee);
        foreach (var hour in new[] { 9, 11, 13 })
        {
            await BookAsync(token, scene, AppointmentScene.Day, new TimeOnly(hour, 0));
        }

        var first = await HistoryAsync(token, scene.Customer.Id, "page=1&pageSize=2");
        var second = await HistoryAsync(token, scene.Customer.Id, "page=2&pageSize=2");

        first.Data.GetProperty("items").GetArrayLength().Should().Be(2);
        second.Data.GetProperty("items").GetArrayLength().Should().Be(1);
        var pagination = first.Body.GetProperty("meta").GetProperty("pagination");
        pagination.GetProperty("totalCount").GetInt32().Should().Be(3);
        pagination.GetProperty("totalPages").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Una_clienta_sin_citas_tiene_un_historial_vacio_y_una_de_baja_conserva_el_suyo()
    {
        var scene = await factory.CreateAppointmentSceneAsync(TestData.OrgA);
        var token = await StaffTokenAsync(Roles.Employee);
        var withoutAppointments = await factory.CreateCustomerAsync(TestData.OrgA);
        await BookAsync(token, scene, AppointmentScene.Day, new TimeOnly(10, 0));
        await using (var scope = factory.CreateTenantScope(TestData.OrgA))
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Customers.SingleAsync(c => c.Id == scene.Customer.Id)).IsActive = false;
            await db.SaveChangesAsync();
        }

        var empty = await HistoryAsync(token, withoutAppointments.Id);
        var deactivated = await HistoryAsync(token, scene.Customer.Id);

        empty.Status.Should().Be(HttpStatusCode.OK);
        empty.Data.GetProperty("items").GetArrayLength().Should().Be(0);
        deactivated.Status.Should().Be(HttpStatusCode.OK);
        deactivated.Data.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Una_clienta_inexistente_o_de_otro_centro_da_404()
    {
        var customerB = await factory.CreateCustomerAsync(TestData.OrgB);
        var token = await StaffTokenAsync(Roles.Admin);

        var missing = await HistoryAsync(token, 999999);
        var foreign = await HistoryAsync(token, customerB.Id);

        foreach (var result in new[] { missing, foreign })
        {
            result.Status.Should().Be(HttpStatusCode.NotFound);
            result.ShouldBeEnvelope(success: false);
            result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
        }
    }

    [Fact]
    public async Task Una_clienta_no_accede_al_historial_ni_siquiera_al_suyo()
    {
        // El módulo de clientas es de gestión: la clienta ve sus citas en /appointments.
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, customer.Id);

        var result = await HistoryAsync(token, customer.Id);

        result.Status.Should().Be(HttpStatusCode.Forbidden);
        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
    }

    private Task<ApiResult> HistoryAsync(string token, int customerId, string query = "") =>
        factory.SendAsync(HttpMethod.Get, $"/api/v1/customers/{customerId}/history?{query}", TestData.OrgA, token);

    private async Task<int> BookAsync(string token, AppointmentScene scene, DateOnly day, TimeOnly start)
    {
        var body = scene.Tinting(start) with { AppointmentDate = day };
        var result = await factory.SendAsync(HttpMethod.Post, "/api/v1/appointments", TestData.OrgA, token, body);
        result.Status.Should().Be(HttpStatusCode.Created, result.Body.ToString());
        return result.Data.GetProperty("id").GetInt32();
    }

    private async Task<string> StaffTokenAsync(string rol)
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA, rol);
        return await factory.TokenForAsync(TestData.OrgA, employee.Id);
    }
}
