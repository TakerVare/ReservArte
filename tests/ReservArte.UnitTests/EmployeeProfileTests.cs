using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Application.Mapping;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Mapeos del módulo de Empleados. Un mapeo incompleto no rompe la
/// compilación: falla en runtime al primer uso, así que se valida aquí.
/// </summary>
public class EmployeeProfileTests
{
    private static MapperConfiguration CreateConfiguration() =>
        new(cfg => cfg.AddProfile<EmployeeProfile>(), NullLoggerFactory.Instance);

    [Fact]
    public void La_configuracion_de_mapeo_es_valida()
    {
        // Detecta destinos sin origen: si se añade un campo al DTO y se olvida
        // mapearlo, este test cae.
        CreateConfiguration().AssertConfigurationIsValid();
    }

    [Fact]
    public void Employee_se_mapea_a_EmployeeDto_con_el_nombre_completo()
    {
        var mapper = CreateConfiguration().CreateMapper();

        var dto = mapper.Map<EmployeeDto>(new Employee
        {
            Id = 7,
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
            Rol = "employee",
            IsActive = true,
            HireDate = new DateOnly(2026, 1, 15),
        });

        dto.Id.Should().Be(7);
        dto.FullName.Should().Be("María Salas");
        dto.Email.Should().Be("maria@reservarte.com");
        dto.HireDate.Should().Be(new DateOnly(2026, 1, 15));
    }

    [Fact]
    public void El_nombre_completo_no_deja_espacios_sueltos_si_faltan_apellidos()
    {
        var mapper = CreateConfiguration().CreateMapper();

        var dto = mapper.Map<EmployeeDto>(new Employee { FirstName = "María", LastName = "" });

        dto.FullName.Should().Be("María");
    }

    [Fact]
    public void EmployeeAvailability_se_mapea_conservando_la_convencion_de_dia()
    {
        var mapper = CreateConfiguration().CreateMapper();

        var dto = mapper.Map<EmployeeAvailabilityDto>(new EmployeeAvailability
        {
            Id = 3,
            DayOfWeek = WeekDay.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(18, 0),
        });

        dto.DayOfWeek.Should().Be(WeekDay.Monday);
        dto.StartTime.Should().Be(new TimeOnly(9, 0));
    }
}
