using AwesomeAssertions;
using ReservArte.Application.DTOs.Organizations;
using ReservArte.Application.Validators.Organizations;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>Validación de la configuración del centro (RA-869f74u7y).</summary>
public class OrganizationSettingsValidatorTests
{
    private static readonly UpdateOrganizationSettingsRequestValidator Validator = new();

    private static UpdateOrganizationSettingsRequest Valid() => new()
    {
        TimeZone = "Atlantic/Canary",
        CancellationHoursThreshold = 48,
        MaxNoShowsBeforeBlock = 2,
    };

    [Theory]
    [InlineData("Europe/Madrid")]
    [InlineData("Atlantic/Canary")]
    [InlineData("America/Argentina/Buenos_Aires")]
    public void Una_zona_IANA_supera_la_validacion(string timeZone)
    {
        var request = Valid();
        request.TimeZone = timeZone;

        Validator.Validate(request).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Europa/Madrid")]
    [InlineData("Madrid")]
    [InlineData("GMT+1")]
    [InlineData(" Europe/Madrid")]
    [InlineData("Romance Standard Time")]
    public void Una_zona_que_no_es_IANA_se_rechaza_con_UnknownTimeZone(string timeZone)
    {
        var request = Valid();
        request.TimeZone = timeZone;

        var error = Validator.Validate(request).Errors.Should().ContainSingle().Subject;

        error.PropertyName.Should().Be(nameof(UpdateOrganizationSettingsRequest.TimeZone));
        error.ErrorCode.Should().Be(UpdateOrganizationSettingsRequestValidator.UnknownTimeZoneCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void La_zona_horaria_es_obligatoria(string? timeZone)
    {
        var request = Valid();
        request.TimeZone = timeZone;

        Validator.Validate(request).Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(UpdateOrganizationSettingsRequest.TimeZone));
    }

    [Fact]
    public void Los_tres_campos_son_obligatorios_porque_el_PUT_reemplaza_la_configuracion_entera()
    {
        Validator.Validate(new UpdateOrganizationSettingsRequest()).Errors
            .Select(e => e.PropertyName)
            .Should().BeEquivalentTo(
                nameof(UpdateOrganizationSettingsRequest.TimeZone),
                nameof(UpdateOrganizationSettingsRequest.CancellationHoursThreshold),
                nameof(UpdateOrganizationSettingsRequest.MaxNoShowsBeforeBlock));
    }

    [Theory]
    [InlineData(OrganizationSettings.MinCancellationHoursThreshold, true)]
    [InlineData(OrganizationSettings.MaxCancellationHoursThreshold, true)]
    [InlineData(OrganizationSettings.MinCancellationHoursThreshold - 1, false)]
    [InlineData(OrganizationSettings.MaxCancellationHoursThreshold + 1, false)]
    public void El_umbral_de_cancelacion_va_de_0_a_720_horas(int hours, bool valid)
    {
        var request = Valid();
        request.CancellationHoursThreshold = hours;

        Validator.Validate(request).IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData(OrganizationSettings.MinMaxNoShowsBeforeBlock, true)]
    [InlineData(OrganizationSettings.MaxMaxNoShowsBeforeBlock, true)]
    [InlineData(OrganizationSettings.MinMaxNoShowsBeforeBlock - 1, false)]
    [InlineData(OrganizationSettings.MaxMaxNoShowsBeforeBlock + 1, false)]
    public void El_maximo_de_no_presentaciones_va_de_1_a_99(int noShows, bool valid)
    {
        var request = Valid();
        request.MaxNoShowsBeforeBlock = noShows;

        Validator.Validate(request).IsValid.Should().Be(valid);
    }
}
