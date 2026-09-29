using System.Text.Json;
using AwesomeAssertions;
using ReservArte.Shared.Json;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Frontera JSON de las fechas con hora (RA-869f8pmnm): lo que entra con zona sale en UTC exacto,
/// lo que entra sin zona queda sin especificar para que la validación lo rechace, y todo lo que se
/// escribe va en UTC con «Z».
/// </summary>
public class UtcDateTimeJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new UtcDateTimeJsonConverter() },
    };

    private sealed record Payload(DateTime At, DateTime? Optional);

    private static DateTime Read(string text) =>
        JsonSerializer.Deserialize<DateTime>($"\"{text}\"", Options);

    [Fact]
    public void Una_fecha_con_Z_se_lee_como_UTC_sin_cambios()
    {
        var value = Read("2026-11-02T08:00:00Z");

        value.Kind.Should().Be(DateTimeKind.Utc);
        value.Should().Be(new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Una_fecha_con_desplazamiento_se_convierte_al_instante_UTC_exacto()
    {
        // El convertidor por defecto la pasaba a la hora local del servidor (Kind = Local).
        var value = Read("2026-11-02T08:00:00+02:00");

        value.Kind.Should().Be(DateTimeKind.Utc);
        value.Should().Be(new DateTime(2026, 11, 2, 6, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("2026-10-25T02:30:00+02:00", 0)] // primera 02:30 del cambio de horario en España
    [InlineData("2026-10-25T02:30:00+01:00", 1)] // segunda 02:30, una hora después
    public void La_hora_ambigua_del_cambio_de_horario_se_resuelve_por_el_desplazamiento(string text, int utcHour)
    {
        Read(text).Should().Be(new DateTime(2026, 10, 25, utcHour, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Una_fecha_sin_zona_queda_sin_especificar_para_que_la_validacion_la_rechace()
    {
        var value = Read("2026-11-02T08:00:00");

        value.Kind.Should().Be(DateTimeKind.Unspecified);
        value.Should().Be(new DateTime(2026, 11, 2, 8, 0, 0));
    }

    [Fact]
    public void Un_texto_que_no_es_una_fecha_ISO_falla()
    {
        var act = () => Read("02/11/2026");

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Las_fechas_se_escriben_siempre_en_UTC_con_Z()
    {
        var utc = new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);

        // Unspecified es lo que devuelve SQL Server: es UTC por construcción.
        JsonSerializer.Serialize(utc, Options).Should().Be("\"2026-11-02T08:00:00Z\"");
        JsonSerializer.Serialize(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified), Options)
            .Should().Be("\"2026-11-02T08:00:00Z\"");
        JsonSerializer.Serialize(utc.ToLocalTime(), Options).Should().Be("\"2026-11-02T08:00:00Z\"");
    }

    [Fact]
    public void Las_fechas_opcionales_usan_el_mismo_conversor()
    {
        var payload = JsonSerializer.Deserialize<Payload>(
            "{\"At\":\"2026-11-02T08:00:00+01:00\",\"Optional\":\"2026-11-03T08:00:00+01:00\"}", Options)!;

        payload.At.Should().Be(new DateTime(2026, 11, 2, 7, 0, 0, DateTimeKind.Utc));
        payload.Optional.Should().Be(new DateTime(2026, 11, 3, 7, 0, 0, DateTimeKind.Utc));
        JsonSerializer.Serialize(payload with { Optional = null }, Options)
            .Should().Be("{\"At\":\"2026-11-02T07:00:00Z\",\"Optional\":null}");
    }
}
