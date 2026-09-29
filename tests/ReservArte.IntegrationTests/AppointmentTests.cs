using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Agenda sobre PostgreSQL real: filtros y orden del repositorio de citas con
/// columnas <c>date</c> y <c>time</c>, y detección de solapes del servicio de
/// disponibilidad. Cada test usa su propia empleada, así que los datos de otros
/// tests no interfieren.
/// </summary>
[Collection(ApiCollection.Name)]
public class AppointmentTests(ApiFactory factory)
{
    // Lejos en el futuro: la consulta de huecos descarta los días pasados.
    private static readonly DateOnly Dia1 = new(2030, 3, 4);
    private static readonly DateOnly Dia2 = new(2030, 3, 5);
    private static readonly DateOnly Dia3 = new(2030, 3, 6);

    [Fact]
    public async Task El_filtro_por_fechas_incluye_los_dos_extremos_y_ordena_de_la_mas_reciente_a_la_mas_antigua()
    {
        var (employee, customer) = await NewAgendaAsync(TestData.OrgA);
        var early = await Add(employee, customer, Dia1, 9, 0);
        var middleLate = await Add(employee, customer, Dia2, 16, 0);
        var middleEarly = await Add(employee, customer, Dia2, 10, 0);
        var last = await Add(employee, customer, Dia3, 9, 0);
        await Add(employee, customer, Dia3.AddDays(1), 9, 0);

        var page = await Repository(TestData.OrgA, repo => repo.GetPagedAsync(new AppointmentFilter
        {
            EmployeeId = employee.Id,
            From = Dia1,
            To = Dia3,
        }));

        page.TotalCount.Should().Be(4);
        page.Items.Select(a => a.Id).Should().Equal(last.Id, middleLate.Id, middleEarly.Id, early.Id);
        page.Items.Should().OnlyContain(a => a.Customer != null && a.Employee != null);
    }

    [Fact]
    public async Task El_filtro_por_estado_y_por_baja_logica_se_aplica_en_la_base()
    {
        var (employee, customer) = await NewAgendaAsync(TestData.OrgA);
        var confirmed = await Add(employee, customer, Dia1, 9, 0, AppointmentStatuses.Confirmed);
        await Add(employee, customer, Dia1, 10, 0);
        var inactive = await Add(employee, customer, Dia1, 11, 0, AppointmentStatuses.Confirmed, isActive: false);

        var byStatus = await Repository(TestData.OrgA, repo => repo.GetPagedAsync(new AppointmentFilter
        {
            EmployeeId = employee.Id,
            Status = AppointmentStatuses.Confirmed,
        }));
        var onlyInactive = await Repository(TestData.OrgA, repo => repo.GetPagedAsync(new AppointmentFilter
        {
            EmployeeId = employee.Id,
            IsActive = false,
        }));

        byStatus.Items.Select(a => a.Id).Should().Equal(confirmed.Id);
        onlyInactive.Items.Select(a => a.Id).Should().Equal(inactive.Id);
    }

    [Fact]
    public async Task El_rango_de_la_agenda_devuelve_las_canceladas_ordenadas_y_con_las_horas_exactas()
    {
        var (employee, customer) = await NewAgendaAsync(TestData.OrgA);
        var second = await Add(employee, customer, Dia1, 12, 15, AppointmentStatuses.CancelledByCustomer);
        var first = await Add(employee, customer, Dia1, 9, 45);

        var agenda = await Repository(TestData.OrgA, repo => repo.GetByDateRangeAsync(Dia1, Dia1, employee.Id));

        agenda.Select(a => a.Id).Should().Equal(first.Id, second.Id);
        agenda[0].StartTime.Should().Be(new TimeOnly(9, 45));
        agenda[0].EndTime.Should().Be(new TimeOnly(10, 30));
        agenda[0].AppointmentDate.Should().Be(Dia1);
    }

    [Fact]
    public async Task Las_citas_de_otro_centro_no_aparecen_ni_por_rango_ni_por_Id()
    {
        var (employeeB, customerB) = await NewAgendaAsync(TestData.OrgB);
        var appointmentB = await factory.CreateAppointmentAsync(
            TestData.OrgB, employeeB, customerB, Dia1, new TimeOnly(9, 0), new TimeOnly(9, 45));

        var agendaFromA = await Repository(TestData.OrgA, repo => repo.GetByDateRangeAsync(Dia1, Dia1, employeeB.Id));
        var byIdFromA = await Repository(TestData.OrgA, repo => repo.GetByIdAsync(appointmentB.Id));
        var byIdFromB = await Repository(TestData.OrgB, repo => repo.GetByIdAsync(appointmentB.Id));

        agendaFromA.Should().BeEmpty();
        byIdFromA.Should().BeNull();
        byIdFromB.Should().NotBeNull();
    }

    [Theory]
    [InlineData(10, 0, 10, 45, false)] // mismo rango
    [InlineData(10, 30, 11, 15, false)] // empieza dentro
    [InlineData(9, 30, 10, 15, false)] // acaba dentro
    [InlineData(9, 0, 12, 0, false)] // la envuelve
    [InlineData(10, 45, 11, 30, true)] // empieza justo al acabar: intervalos semiabiertos
    [InlineData(9, 15, 10, 0, true)] // acaba justo al empezar
    public async Task Un_rango_que_pisa_una_cita_viva_no_se_puede_ocupar(
        int startHour, int startMinute, int endHour, int endMinute, bool expectedAvailable)
    {
        var (employee, customer) = await NewAgendaAsync(TestData.OrgA);
        await Add(employee, customer, Dia1, 10, 0);

        var result = await Availability(TestData.OrgA, service => service.EnsureSlotAvailableAsync(
            employee.Id, Dia1, new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute)));

        result.Success.Should().Be(expectedAvailable);
        if (!expectedAvailable)
        {
            result.ErrorCode.Should().Be(ErrorCodes.AptSlotUnavailable);
        }
    }

    [Fact]
    public async Task Una_cita_cancelada_libera_su_hueco_y_reagendar_no_choca_consigo_misma()
    {
        var (employee, customer) = await NewAgendaAsync(TestData.OrgA);
        await Add(employee, customer, Dia1, 10, 0, AppointmentStatuses.Cancelled);
        var live = await Add(employee, customer, Dia1, 12, 0);

        var overCancelled = await Availability(TestData.OrgA, service => service.EnsureSlotAvailableAsync(
            employee.Id, Dia1, new TimeOnly(10, 0), new TimeOnly(10, 45)));
        var rescheduleSelf = await Availability(TestData.OrgA, service => service.EnsureSlotAvailableAsync(
            employee.Id, Dia1, new TimeOnly(12, 15), new TimeOnly(13, 0), excludeAppointmentId: live.Id));

        overCancelled.Success.Should().BeTrue();
        rescheduleSelf.Success.Should().BeTrue();
    }

    [Fact]
    public async Task La_consulta_de_huecos_por_HTTP_no_ofrece_los_que_pisan_una_cita()
    {
        var (employee, customer) = await NewAgendaAsync(TestData.OrgA);
        await Add(employee, customer, Dia1, 10, 0);
        var token = await factory.LoginAsync(TestData.AdminA, TestData.OrgA);

        var result = await factory.SendAsync(HttpMethod.Get,
            $"/api/v1/appointments/availability?employeeId={employee.Id}&date={Dia1:yyyy-MM-dd}&durationMinutes=45",
            TestData.OrgA, token);

        result.Status.Should().Be(HttpStatusCode.OK);
        var starts = result.Data.GetProperty("slots").EnumerateArray()
            .Select(s => s.GetProperty("startTime").GetString()!)
            .ToList();
        starts.Should().Contain(["09:15:00", "10:45:00"]);
        starts.Should().NotContain(["09:30:00", "10:00:00", "10:30:00"]);
    }

    private async Task<(Employee Employee, Customer Customer)> NewAgendaAsync(Guid organizationId) =>
        (await factory.CreateEmployeeAsync(organizationId), await factory.CreateCustomerAsync(organizationId));

    /// <summary>Cita de 45 minutos en el centro A.</summary>
    private Task<Appointment> Add(
        Employee employee, Customer customer, DateOnly date, int hour, int minute,
        string status = AppointmentStatuses.Pending, bool isActive = true)
    {
        var start = new TimeOnly(hour, minute);
        return factory.CreateAppointmentAsync(
            TestData.OrgA, employee, customer, date, start, start.AddMinutes(45), status, isActive);
    }

    private async Task<T> Repository<T>(Guid organizationId, Func<IAppointmentRepository, Task<T>> query)
    {
        await using var scope = factory.CreateTenantScope(organizationId);
        return await query(scope.ServiceProvider.GetRequiredService<IAppointmentRepository>());
    }

    private async Task<T> Availability<T>(Guid organizationId, Func<IAvailabilityService, Task<T>> query)
    {
        await using var scope = factory.CreateTenantScope(organizationId);
        return await query(scope.ServiceProvider.GetRequiredService<IAvailabilityService>());
    }
}
