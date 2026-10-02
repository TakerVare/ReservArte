using AwesomeAssertions;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Application.Validators.Customers;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>Validadores de la ficha completa de la clienta (4.2b): alergias y bloqueo.</summary>
public class CustomerRecordValidatorTests
{
    [Theory]
    [InlineData("Tinte PPD", AllergySeverities.High, true)]
    [InlineData("", AllergySeverities.Low, false)]
    [InlineData("Látex", "letal", false)]
    [InlineData("Látex", "", false)]
    public void La_alergia_exige_descripcion_y_una_gravedad_del_catalogo(string description, string severity, bool valid) =>
        new CustomerAllergyRequestValidator()
            .Validate(new CustomerAllergyRequest { AllergyDescription = description, Severity = severity })
            .IsValid.Should().Be(valid);

    [Fact]
    public void La_descripcion_de_la_alergia_cabe_en_su_columna() =>
        new CustomerAllergyRequestValidator()
            .Validate(new CustomerAllergyRequest { AllergyDescription = new string('a', 501), Severity = AllergySeverities.Low })
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData("Impagos", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void El_bloqueo_exige_motivo(string reason, bool valid) =>
        new BlockCustomerRequestValidator()
            .Validate(new BlockCustomerRequest { Reason = reason })
            .IsValid.Should().Be(valid);

    [Fact]
    public void El_motivo_del_bloqueo_cabe_en_su_columna() =>
        new BlockCustomerRequestValidator()
            .Validate(new BlockCustomerRequest { Reason = new string('a', 501) })
            .IsValid.Should().BeFalse();
}
