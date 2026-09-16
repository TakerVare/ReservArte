using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ReservArte.Application.DTOs.Services;
using ReservArte.Application.Mapping;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Mapeos del catálogo (RA-869d7f3z0). Como en Empleados y Clientes, un mapeo
/// incompleto no rompe la compilación: falla en runtime al primer uso.
/// </summary>
public class ServiceCatalogProfileTests
{
    private static MapperConfiguration CreateConfiguration() =>
        new(cfg => cfg.AddProfile<ServiceCatalogProfile>(), NullLoggerFactory.Instance);

    [Fact]
    public void La_configuracion_de_mapeo_es_valida()
    {
        CreateConfiguration().AssertConfigurationIsValid();
    }

    [Fact]
    public void El_servicio_expone_el_nombre_de_su_categoria()
    {
        var mapper = CreateConfiguration().CreateMapper();

        var dto = mapper.Map<ServiceDto>(new Service
        {
            Id = 1,
            Name = "Diseño de cejas",
            DurationMinutes = 45,
            BasePrice = 25.00m,
            CategoryId = 7,
            Category = new ServiceCategory { Id = 7, Name = "Cejas" },
        });

        dto.CategoryName.Should().Be("Cejas");
        dto.CategoryId.Should().Be(7);
    }

    [Fact]
    public void Un_servicio_sin_categoria_se_mapea_con_el_nombre_nulo()
    {
        // La categoría es opcional; sin el cuidado del null, el mapeo lanzaría
        // al resolver Category.Name.
        var mapper = CreateConfiguration().CreateMapper();

        var dto = mapper.Map<ServiceDto>(new Service
        {
            Id = 2,
            Name = "Consulta previa",
            DurationMinutes = 15,
            CategoryId = null,
        });

        dto.CategoryName.Should().BeNull();
        dto.CategoryId.Should().BeNull();
    }

    [Fact]
    public void El_detalle_incluye_variaciones_y_tarifas()
    {
        var mapper = CreateConfiguration().CreateMapper();

        var dto = mapper.Map<ServiceDetailDto>(new Service
        {
            Id = 1,
            Name = "Diseño de cejas",
            DurationMinutes = 45,
            BasePrice = 25.00m,
            Variations =
            {
                new ServiceVariation { Id = 10, Name = "Con hilo", PriceModifier = 5m, DurationModifier = 15 },
            },
            Pricings =
            {
                new ServicePricing { Id = 20, EmployeeLevel = EmployeeLevels.Senior, Price = 25.00m },
            },
        });

        dto.Name.Should().Be("Diseño de cejas");
        dto.Variations.Should().ContainSingle().Which.DurationModifier.Should().Be(15);
        dto.Pricings.Should().ContainSingle().Which.EmployeeLevel.Should().Be(EmployeeLevels.Senior);
    }
}
