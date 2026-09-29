using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReservArte.Shared.Json;

/// <summary>
/// Frontera JSON de las fechas con hora (RA-869f8pmnm): todas las marcas de tiempo de la API son
/// UTC (vol. 1 §5.1).
/// - Al leer: una fecha con zona («Z» o desplazamiento como «+02:00») se convierte al instante
///   UTC exacto (Kind = Utc). El convertidor por defecto la pasaba a la hora local del servidor
///   (Kind = Local), y SQL Server la guardaba desplazada. Una fecha sin zona se deja como
///   Kind = Unspecified para que la validación la rechace con 400: sin zona es ambigua.
/// - Al escribir: siempre en UTC con «Z». Lo que sale de la base de datos llega como Unspecified
///   (SQL Server no guarda la zona) y es UTC por construcción, así que se marca como tal; sin la
///   «Z», un navegador lo interpretaría como hora local.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.TryGetDateTime(out var parsed))
        {
            throw new JsonException("La fecha no tiene formato ISO 8601.");
        }

        // Sin zona: se conserva tal cual, marcada como Unspecified, para que la rechace la validación.
        if (parsed.Kind == DateTimeKind.Unspecified)
        {
            return parsed;
        }

        // Con zona: se relee como DateTimeOffset, que conserva el desplazamiento del texto, y se toma
        // su instante UTC. Pasar por la hora local (ToUniversalTime) fallaría en la hora ambigua del
        // cambio de horario.
        return reader.GetDateTimeOffset().UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        writer.WriteStringValue(utc);
    }
}
