using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Las ausencias se guardan en UTC (<c>timestamptz</c>, RA-869f8pmnm) y el horario
/// y las citas en hora local del centro (<c>Europe/Madrid</c>). La disponibilidad
/// tiene que pasar las ausencias a hora local antes de recortarlas al día; si no,
/// se desplazan una hora en invierno y dos en verano.
/// </summary>
[Collection(ApiCollection.Name)]
public class AvailabilityTimeZoneTests(ApiFactory factory)
{
    private static readonly DateOnly Invierno = new(2030, 1, 14);
    private static readonly DateOnly Verano = new(2030, 7, 15);

    [Theory]
    [MemberData(nameof(Dias))]
    public async Task Una_ausencia_de_10_a_11_hora_de_Madrid_bloquea_ese_tramo_y_no_otro(DateOnly day, int utcOffset)
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA);
        // 10:00-11:00 en Madrid, guardada en UTC como lo hace la API.
        await AddExceptionAsync(employee,
            day.ToDateTime(new TimeOnly(10 - utcOffset, 0), DateTimeKind.Utc),
            day.ToDateTime(new TimeOnly(11 - utcOffset, 0), DateTimeKind.Utc));

        (await IsFreeAsync(employee, day, 9, 0, 10, 0)).Should().BeTrue("acaba justo cuando empieza la ausencia");
        (await IsFreeAsync(employee, day, 10, 0, 10, 45)).Should().BeFalse();
        (await IsFreeAsync(employee, day, 10, 45, 11, 30)).Should().BeFalse();
        (await IsFreeAsync(employee, day, 11, 0, 11, 45)).Should().BeTrue("empieza justo cuando acaba la ausencia");
    }

    public static TheoryData<DateOnly, int> Dias => new()
    {
        { Invierno, 1 },
        { Verano, 2 },
    };

    [Fact]
    public async Task Una_ausencia_que_empieza_la_noche_anterior_bloquea_la_manana_hasta_su_fin()
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA);
        // Del domingo a las 22:00 al lunes a las 09:30, hora de Madrid (invierno, UTC+1).
        await AddExceptionAsync(employee,
            Invierno.AddDays(-1).ToDateTime(new TimeOnly(21, 0), DateTimeKind.Utc),
            Invierno.ToDateTime(new TimeOnly(8, 30), DateTimeKind.Utc));

        (await IsFreeAsync(employee, Invierno, 9, 0, 9, 45)).Should().BeFalse();
        (await IsFreeAsync(employee, Invierno, 9, 30, 10, 15)).Should().BeTrue();
    }

    [Fact]
    public async Task Una_ausencia_que_acaba_antes_de_la_medianoche_local_no_toca_el_dia_siguiente()
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA);
        // Lunes de 17:00 a 23:30 en Madrid (invierno): 16:00Z-22:30Z. En UTC acaba
        // el lunes, pero su última media hora local no es del martes.
        await AddExceptionAsync(employee,
            Invierno.ToDateTime(new TimeOnly(16, 0), DateTimeKind.Utc),
            Invierno.ToDateTime(new TimeOnly(22, 30), DateTimeKind.Utc));

        (await IsFreeAsync(employee, Invierno, 17, 0, 17, 45)).Should().BeFalse();
        (await IsFreeAsync(employee, Invierno.AddDays(1), 9, 0, 9, 45)).Should().BeTrue();
    }

    private async Task AddExceptionAsync(Employee employee, DateTime startUtc, DateTime endUtc)
    {
        await using var scope = factory.CreateTenantScope(TestData.OrgA);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.EmployeeExceptions.Add(new EmployeeException
        {
            OrganizationId = TestData.OrgA,
            EmployeeId = employee.Id,
            StartDateTime = startUtc,
            EndDateTime = endUtc,
            Type = EmployeeExceptionTypes.Vacation,
        });
        await db.SaveChangesAsync();
    }

    private async Task<bool> IsFreeAsync(
        Employee employee, DateOnly day, int startHour, int startMinute, int endHour, int endMinute)
    {
        await using var scope = factory.CreateTenantScope(TestData.OrgA);
        var result = await scope.ServiceProvider.GetRequiredService<IAvailabilityService>()
            .EnsureSlotAvailableAsync(
                employee.Id, day, new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));

        if (!result.Success)
        {
            result.ErrorCode.Should().Be(ErrorCodes.AptSlotUnavailable);
        }

        return result.Success;
    }
}
