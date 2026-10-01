using ReservArte.Application.DTOs.Appointments;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Intervalo semiabierto <c>[Start, End)</c> en minutos desde medianoche.
/// Semiabierto para que dos citas contiguas (11:00-12:00 y 12:00-13:00) no se
/// consideren solapadas.
/// </summary>
internal readonly record struct MinuteRange(int Start, int End)
{
    public bool Overlaps(MinuteRange other) => Start < other.End && other.Start < End;

    public bool Contains(MinuteRange other) => other.Start >= Start && other.End <= End;
}

/// <summary>
/// Rejilla de huecos de un día (RA-869d7f4rd), compartida por la disponibilidad de
/// un empleado y la de un servicio (RA-869fagpx9) para que las dos ofrezcan
/// exactamente los mismos huecos.
///
/// Todo va en minutos desde medianoche y no sobre <see cref="TimeOnly"/>:
/// `TimeOnly.AddMinutes` da la vuelta al pasar de las 23:59.
/// </summary>
internal static class SlotGrid
{
    public const int MinutesPerDay = 24 * 60;

    /// <summary>
    /// Huecos de <paramref name="durationMinutes"/> dentro de los tramos del horario
    /// que no pisan nada ocupado y empiezan a partir de <paramref name="notBefore"/>.
    /// La rejilla se ancla al inicio de cada tramo, no a la hora actual, para que los
    /// huecos caigan siempre en los mismos minutos del reloj.
    /// </summary>
    public static List<TimeSlotDto> Compute(
        IReadOnlyList<MinuteRange> schedule,
        IReadOnlyList<MinuteRange> busy,
        int notBefore,
        int durationMinutes,
        int stepMinutes)
    {
        var slots = new List<TimeSlotDto>();

        foreach (var window in schedule)
        {
            var start = Math.Max(window.Start, notBefore);
            if (start > window.Start)
            {
                var stepsSkipped = (start - window.Start + stepMinutes - 1) / stepMinutes;
                start = window.Start + (stepsSkipped * stepMinutes);
            }

            for (var from = start; from + durationMinutes <= window.End; from += stepMinutes)
            {
                var candidate = new MinuteRange(from, from + durationMinutes);
                if (busy.Any(b => b.Overlaps(candidate)))
                {
                    continue;
                }

                slots.Add(new TimeSlotDto
                {
                    StartTime = ToTimeOnly(candidate.Start),
                    EndTime = ToTimeOnly(candidate.End),
                });
            }
        }

        // Dos tramos no deberían solaparse —el PUT de disponibilidad lo valida—, pero
        // ordenar y deduplicar evita ofrecer dos veces el mismo hueco si alguno se coló
        // por SQL.
        return slots
            .DistinctBy(s => s.StartTime)
            .OrderBy(s => s.StartTime)
            .ToList();
    }

    public static int ToMinutes(TimeOnly time) => (time.Hour * 60) + time.Minute;

    public static TimeOnly ToTimeOnly(int minutes) => new(minutes / 60, minutes % 60);

    /// <summary>Instante UTC de la base, en hora local del centro.</summary>
    public static DateTime ToLocal(DateTime utcInstant, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc), timeZone);

    /// <summary>
    /// Pasa un instante a minutos dentro del día indicado, recortando lo que se salga
    /// por cualquiera de los dos extremos.
    /// </summary>
    public static int ClampToDay(DateTime instant, DateTime dayStart, bool floor)
    {
        var minutes = (int)Math.Round((instant - dayStart).TotalMinutes, MidpointRounding.ToZero);

        return floor
            ? Math.Max(minutes, 0)
            : Math.Min(minutes, MinutesPerDay);
    }
}
