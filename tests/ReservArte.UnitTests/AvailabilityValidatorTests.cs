using FluentAssertions;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Application.Validators.Employees;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Reglas del horario semanal y de las ausencias (RA-869d7f01b). Varias son
/// espejo de CHECKs del esquema: validarlas aquí convierte el fallo en
/// GEN_VALIDATION_FAILED en vez de en un error de base de datos.
/// </summary>
public class AvailabilityValidatorTests
{
    private readonly UpdateAvailabilityRequestValidator _scheduleValidator = new();
    private readonly CreateEmployeeExceptionRequestValidator _exceptionValidator = new();

    private static AvailabilitySlotRequest Slot(int day, string start, string end) => new()
    {
        DayOfWeek = day,
        StartTime = TimeOnly.Parse(start),
        EndTime = TimeOnly.Parse(end),
    };

    private static UpdateAvailabilityRequest ScheduleOf(params AvailabilitySlotRequest[] slots) =>
        new() { WeeklySchedule = slots };

    // ── Horario semanal ───────────────────────────────────────────────────

    [Fact]
    public void Una_jornada_partida_el_mismo_dia_es_valida()
    {
        var request = ScheduleOf(
            Slot(WeekDay.Monday, "09:00", "14:00"),
            Slot(WeekDay.Monday, "16:00", "20:00"));

        _scheduleValidator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Un_horario_vacio_es_valido()
    {
        _scheduleValidator.Validate(ScheduleOf()).IsValid.Should().BeTrue(
            "vaciar el horario es la forma de dejar al empleado sin disponibilidad");
    }

    [Fact]
    public void Dos_tramos_que_se_solapan_el_mismo_dia_fallan()
    {
        var request = ScheduleOf(
            Slot(WeekDay.Tuesday, "09:00", "14:00"),
            Slot(WeekDay.Tuesday, "13:30", "18:00"));

        _scheduleValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Un_tramo_que_empieza_justo_al_acabar_el_anterior_no_es_solape()
    {
        var request = ScheduleOf(
            Slot(WeekDay.Tuesday, "09:00", "14:00"),
            Slot(WeekDay.Tuesday, "14:00", "18:00"));

        _scheduleValidator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Tramos_a_la_misma_hora_en_dias_distintos_no_son_solape()
    {
        var request = ScheduleOf(
            Slot(WeekDay.Monday, "09:00", "14:00"),
            Slot(WeekDay.Wednesday, "09:00", "14:00"));

        _scheduleValidator.Validate(request).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("14:00", "09:00")]
    [InlineData("10:00", "10:00")]
    public void El_fin_debe_ser_posterior_al_inicio(string start, string end)
    {
        var request = ScheduleOf(Slot(WeekDay.Monday, start, end));

        _scheduleValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void El_dia_debe_estar_entre_0_y_6(int day)
    {
        var request = ScheduleOf(Slot(day, "09:00", "14:00"));

        _scheduleValidator.Validate(request).IsValid.Should().BeFalse(
            "es el espejo de CK_EmployeeAvailabilities_DayOfWeek");
    }

    [Fact]
    public void No_se_admiten_mas_tramos_que_el_maximo()
    {
        // Todos el mismo día y sin solape: lo que falla es el número, no la forma.
        var slots = Enumerable
            .Range(0, UpdateAvailabilityRequestValidator.MaxSlots + 1)
            .Select(i => new AvailabilitySlotRequest
            {
                DayOfWeek = WeekDay.Monday,
                StartTime = new TimeOnly(0, 0).AddMinutes(i * 2),
                EndTime = new TimeOnly(0, 1).AddMinutes(i * 2),
            })
            .ToArray();

        _scheduleValidator.Validate(ScheduleOf(slots)).IsValid.Should().BeFalse();
    }

    // ── Ausencias ─────────────────────────────────────────────────────────

    private static CreateEmployeeExceptionRequest ValidException() => new()
    {
        StartDateTime = new DateTime(2026, 12, 24),
        EndDateTime = new DateTime(2026, 12, 26),
        Type = EmployeeExceptionTypes.Vacation,
        Reason = "Navidad",
    };

    [Fact]
    public void Una_ausencia_completa_es_valida()
    {
        _exceptionValidator.Validate(ValidException()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Una_ausencia_con_el_intervalo_invertido_falla()
    {
        var request = new CreateEmployeeExceptionRequest
        {
            StartDateTime = new DateTime(2026, 12, 26),
            EndDateTime = new DateTime(2026, 12, 24),
            Type = EmployeeExceptionTypes.Vacation,
        };

        _exceptionValidator.Validate(request).IsValid.Should().BeFalse(
            "es el espejo de CK_EmployeeExceptions_Interval");
    }

    [Theory]
    [InlineData("")]
    [InlineData("holidays")]
    [InlineData("Vacation")]
    public void El_tipo_debe_ser_uno_del_catalogo(string type)
    {
        var request = new CreateEmployeeExceptionRequest
        {
            StartDateTime = new DateTime(2026, 12, 24),
            EndDateTime = new DateTime(2026, 12, 26),
            Type = type,
        };

        _exceptionValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Todos_los_tipos_del_catalogo_se_admiten()
    {
        foreach (var type in EmployeeExceptionTypes.All)
        {
            var request = new CreateEmployeeExceptionRequest
            {
                StartDateTime = new DateTime(2026, 12, 24),
                EndDateTime = new DateTime(2026, 12, 26),
                Type = type,
            };

            _exceptionValidator.Validate(request).IsValid.Should().BeTrue($"«{type}» está en el CHECK");
        }
    }

    [Fact]
    public void El_motivo_no_puede_superar_la_longitud_de_la_columna()
    {
        var request = new CreateEmployeeExceptionRequest
        {
            StartDateTime = new DateTime(2026, 12, 24),
            EndDateTime = new DateTime(2026, 12, 26),
            Type = EmployeeExceptionTypes.Other,
            Reason = new string('x', 501),
        };

        _exceptionValidator.Validate(request).IsValid.Should().BeFalse();
    }
}
