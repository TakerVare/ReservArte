using AwesomeAssertions;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Services;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Hora de envío de un recordatorio (RA-869d7f5zq): la cita va en hora local del
/// centro y el resultado, en UTC.
/// </summary>
public class ReminderScheduleTests
{
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
    private static readonly TimeZoneInfo Canarias = TimeZoneInfo.FindSystemTimeZoneById("Atlantic/Canary");

    /// <summary>Lunes de invierno (UTC+1 en la península).</summary>
    private static readonly DateOnly Invierno = new(2031, 3, 3);

    /// <summary>Lunes de verano (UTC+2 en la península).</summary>
    private static readonly DateOnly Verano = new(2031, 7, 7);

    private static ReminderConfiguration Reminder(int hours, int? windowStart = null, int? windowEnd = null) => new()
    {
        HoursBeforeAppointment = hours,
        AllowedSendStartTime = windowStart is { } s ? new TimeOnly(s, 0) : null,
        AllowedSendEndTime = windowEnd is { } e ? new TimeOnly(e, 0) : null,
    };

    private static DateTime Utc(int year, int month, int day, int hour, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    [Fact]
    public void Sale_la_antelacion_configurada_antes_de_la_cita_en_hora_del_centro()
    {
        ReminderSchedule.SendAtUtc(Invierno, new TimeOnly(10, 30), Reminder(24), Madrid)
            .Should().Be(Utc(2031, 3, 2, 9, 30));
        ReminderSchedule.SendAtUtc(Verano, new TimeOnly(10, 30), Reminder(24), Madrid)
            .Should().Be(Utc(2031, 7, 6, 8, 30));
    }

    [Fact]
    public void El_resultado_es_un_instante_en_UTC()
    {
        ReminderSchedule.SendAtUtc(Invierno, new TimeOnly(10, 0), Reminder(2), Madrid)!.Value.Kind
            .Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void La_misma_cita_en_Canarias_sale_una_hora_mas_tarde_en_UTC()
    {
        ReminderSchedule.SendAtUtc(Invierno, new TimeOnly(10, 0), Reminder(2), Madrid)
            .Should().Be(Utc(2031, 3, 3, 7));
        ReminderSchedule.SendAtUtc(Invierno, new TimeOnly(10, 0), Reminder(2), Canarias)
            .Should().Be(Utc(2031, 3, 3, 8));
    }

    [Fact]
    public void Dentro_de_la_franja_de_envio_no_se_mueve()
    {
        ReminderSchedule.SendAtUtc(Invierno, new TimeOnly(12, 0), Reminder(2, 9, 21), Madrid)
            .Should().Be(Utc(2031, 3, 3, 9));
    }

    [Fact]
    public void Antes_de_la_franja_espera_a_que_empiece_ese_mismo_dia()
    {
        // Cita a las 10:00 con aviso 2 h antes: tocaría a las 08:00; sale a las 09:00.
        ReminderSchedule.SendAtUtc(Invierno, new TimeOnly(10, 0), Reminder(2, 9, 21), Madrid)
            .Should().Be(Utc(2031, 3, 3, 8));
    }

    [Fact]
    public void Despues_de_la_franja_se_adelanta_al_inicio_de_ese_dia()
    {
        // Cita el lunes a las 09:30 con aviso 12 h antes: tocaría el domingo a las
        // 21:30, fuera de la franja; sale el domingo a las 09:00.
        ReminderSchedule.SendAtUtc(Invierno, new TimeOnly(9, 30), Reminder(12, 9, 21), Madrid)
            .Should().Be(Utc(2031, 3, 2, 8));
    }

    [Fact]
    public void El_final_de_la_franja_ya_queda_fuera()
    {
        // Tocaría a las 21:00 en punto: la franja es [09:00, 21:00).
        ReminderSchedule.SendAtUtc(Invierno, new TimeOnly(9, 0), Reminder(12, 9, 21), Madrid)
            .Should().Be(Utc(2031, 3, 2, 8));
    }

    [Fact]
    public void Si_el_ajuste_lo_deja_a_la_hora_de_la_cita_o_despues_no_se_envia()
    {
        // Cita a las 09:00 con aviso 1 h antes y franja desde las 09:00: el aviso
        // saldría justo cuando empieza la cita.
        ReminderSchedule.SendAtUtc(Invierno, new TimeOnly(9, 0), Reminder(1, 9, 21), Madrid)
            .Should().BeNull();
        ReminderSchedule.SendAtUtc(Invierno, new TimeOnly(8, 0), Reminder(1, 9, 21), Madrid)
            .Should().BeNull();
    }

    [Fact]
    public void El_cambio_de_hora_entre_el_aviso_y_la_cita_no_desplaza_la_hora_local()
    {
        // El domingo 30 de marzo de 2031 empieza el horario de verano. Cita el
        // lunes 31 a las 10:00 (UTC+2), aviso 48 h antes: sábado 29 a las 10:00 (UTC+1).
        ReminderSchedule.SendAtUtc(new DateOnly(2031, 3, 31), new TimeOnly(10, 0), Reminder(48), Madrid)
            .Should().Be(Utc(2031, 3, 29, 9));
    }

    [Fact]
    public void Una_hora_local_que_no_existe_se_toma_una_hora_despues()
    {
        // Las 02:30 del 30 de marzo de 2031 no existen en la península: el reloj
        // pasa de las 02:00 a las 03:00. Cita a las 08:30, aviso 6 h antes.
        ReminderSchedule.SendAtUtc(new DateOnly(2031, 3, 30), new TimeOnly(8, 30), Reminder(6), Madrid)
            .Should().Be(Utc(2031, 3, 30, 1, 30));
    }
}
