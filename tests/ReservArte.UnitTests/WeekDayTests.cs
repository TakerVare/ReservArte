using FluentAssertions;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Tests de la conversión entre <see cref="System.DayOfWeek"/> (domingo = 0) y
/// la convención del proyecto para <c>EmployeeAvailability.DayOfWeek</c>
/// (lunes = 0). Es una unidad pura, pero el desfase de un día entre ambas
/// convenciones es justo el error que estos tests existen para impedir.
/// </summary>
public class WeekDayTests
{
    [Theory]
    [InlineData(DayOfWeek.Monday, WeekDay.Monday)]
    [InlineData(DayOfWeek.Tuesday, WeekDay.Tuesday)]
    [InlineData(DayOfWeek.Wednesday, WeekDay.Wednesday)]
    [InlineData(DayOfWeek.Thursday, WeekDay.Thursday)]
    [InlineData(DayOfWeek.Friday, WeekDay.Friday)]
    [InlineData(DayOfWeek.Saturday, WeekDay.Saturday)]
    [InlineData(DayOfWeek.Sunday, WeekDay.Sunday)]
    public void FromDayOfWeek_traduce_la_semana_completa(DayOfWeek input, int expected)
    {
        WeekDay.FromDayOfWeek(input).Should().Be(expected);
    }

    [Fact]
    public void La_semana_del_proyecto_empieza_en_lunes_y_acaba_en_domingo()
    {
        WeekDay.Monday.Should().Be(0);
        WeekDay.Sunday.Should().Be(6);
    }

    [Theory]
    [InlineData(WeekDay.Monday)]
    [InlineData(WeekDay.Tuesday)]
    [InlineData(WeekDay.Wednesday)]
    [InlineData(WeekDay.Thursday)]
    [InlineData(WeekDay.Friday)]
    [InlineData(WeekDay.Saturday)]
    [InlineData(WeekDay.Sunday)]
    public void ToDayOfWeek_es_la_inversa_de_FromDayOfWeek(int projectDay)
    {
        WeekDay.FromDayOfWeek(WeekDay.ToDayOfWeek(projectDay)).Should().Be(projectDay);
    }

    [Fact]
    public void FromDate_usa_la_convencion_del_proyecto()
    {
        // 2026-09-14 es lunes; 2026-09-20, el domingo de esa misma semana.
        WeekDay.FromDate(new DateTime(2026, 9, 14)).Should().Be(WeekDay.Monday);
        WeekDay.FromDate(new DateTime(2026, 9, 20)).Should().Be(WeekDay.Sunday);
    }

    [Fact]
    public void FromDate_trata_igual_DateOnly_y_DateTime()
    {
        var fecha = new DateTime(2026, 9, 16); // miércoles

        WeekDay.FromDate(fecha).Should().Be(WeekDay.Wednesday);
        WeekDay.FromDate(DateOnly.FromDateTime(fecha)).Should().Be(WeekDay.Wednesday);
    }
}
