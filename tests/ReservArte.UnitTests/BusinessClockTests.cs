using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Services;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Zona horaria del centro (RA-869f74u7y): sale de su configuración y, sin fila,
/// es la de la península.
/// </summary>
public class BusinessClockTests
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private readonly Mock<IOrganizationSettingsRepository> _settings = new();
    private readonly Mock<ICurrentOrganizationService> _currentOrganization = new();

    public BusinessClockTests()
    {
        _currentOrganization.SetupGet(o => o.OrganizationId).Returns(OrgA);
    }

    private BusinessClock CreateClock() =>
        new(_settings.Object, _currentOrganization.Object, NullLogger<BusinessClock>.Instance);

    private void SettingsWith(string timeZone) =>
        _settings
            .Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationSettings { TimeZone = timeZone });

    [Fact]
    public async Task Un_centro_sin_configuracion_trabaja_en_la_zona_por_defecto()
    {
        _settings
            .Setup(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationSettings?)null);

        var timeZone = await CreateClock().FindTimeZoneAsync();

        timeZone!.Id.Should().Be("Europe/Madrid");
    }

    [Fact]
    public async Task La_zona_es_la_de_la_configuracion_del_centro()
    {
        SettingsWith("Atlantic/Canary");

        var timeZone = await CreateClock().FindTimeZoneAsync();

        // Canarias va una hora por detrás de la península todo el año.
        timeZone!.Id.Should().Be("Atlantic/Canary");
        timeZone.GetUtcOffset(new DateTime(2030, 1, 14, 12, 0, 0, DateTimeKind.Utc)).Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task Una_zona_que_la_maquina_no_resuelve_devuelve_null()
    {
        SettingsWith("Marte/Olympus_Mons");

        (await CreateClock().FindTimeZoneAsync()).Should().BeNull();
    }

    [Fact]
    public async Task La_zona_se_consulta_una_sola_vez_por_centro()
    {
        SettingsWith("Atlantic/Canary");
        var clock = CreateClock();

        await clock.FindTimeZoneAsync();
        await clock.FindTimeZoneAsync();

        _settings.Verify(r => r.GetCurrentAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Si_el_scope_cambia_de_centro_la_zona_se_vuelve_a_leer()
    {
        SettingsWith("Atlantic/Canary");
        var clock = CreateClock();
        await clock.FindTimeZoneAsync();

        _currentOrganization.SetupGet(o => o.OrganizationId).Returns(OrgB);
        SettingsWith("Europe/Lisbon");

        (await clock.FindTimeZoneAsync())!.Id.Should().Be("Europe/Lisbon");
    }

    [Fact]
    public void Ahora_es_la_hora_local_de_la_zona()
    {
        var utc = new DateTimeOffset(2030, 7, 15, 22, 30, 0, TimeSpan.Zero);

        var madrid = BusinessClock.Now(new FixedTimeProvider(utc), TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"));
        var canarias = BusinessClock.Now(new FixedTimeProvider(utc), TimeZoneInfo.FindSystemTimeZoneById("Atlantic/Canary"));

        // En la península ya es el día siguiente; en Canarias, todavía no.
        madrid.Should().Be(new DateTime(2030, 7, 16, 0, 30, 0));
        canarias.Should().Be(new DateTime(2030, 7, 15, 23, 30, 0));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
