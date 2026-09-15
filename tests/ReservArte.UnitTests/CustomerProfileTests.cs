using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Application.Mapping;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Mapeos del módulo de Clientes. Como en Empleados, un mapeo incompleto no
/// rompe la compilación: falla en runtime al primer uso.
/// </summary>
public class CustomerProfileTests
{
    private static MapperConfiguration CreateConfiguration() =>
        new(cfg => cfg.AddProfile<CustomerProfile>(), NullLoggerFactory.Instance);

    [Fact]
    public void La_configuracion_de_mapeo_es_valida()
    {
        CreateConfiguration().AssertConfigurationIsValid();
    }

    [Fact]
    public void El_perfil_completo_lleva_la_ficha_y_sus_colecciones()
    {
        var mapper = CreateConfiguration().CreateMapper();

        var dto = mapper.Map<CustomerDetailDto>(new Customer
        {
            Id = 4,
            FirstName = "Carmen",
            LastName = "López",
            Email = "carmen.lopez@example.com",
            Category = CustomerCategories.Vip,
            Consents =
            {
                new CustomerConsent { ConsentType = CustomerConsentTypes.DataProcessing, IsGranted = true },
            },
            Allergies = { new CustomerAllergy { Id = 1, AllergyDescription = "Látex", Severity = AllergySeverities.High } },
            Notes = { new CustomerNote { Id = 2, EmployeeId = 2, Note = "Prefiere citas por la tarde." } },
        });

        dto.FullName.Should().Be("Carmen López");
        dto.Category.Should().Be(CustomerCategories.Vip);
        dto.Consents.Should().ContainSingle().Which.ConsentType.Should().Be(CustomerConsentTypes.DataProcessing);
        dto.Allergies.Should().ContainSingle().Which.Severity.Should().Be(AllergySeverities.High);
        dto.Notes.Should().ContainSingle().Which.EmployeeId.Should().Be(2);
    }
}
