using FluentAssertions;
using ReservArte.Application.DTOs.Services;
using ReservArte.Application.Validators.Services;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>Validación de entrada del alta y la edición de servicios (RA-869d7f3z0).</summary>
public class ServiceValidatorTests
{
    private static CreateServiceRequest ValidCreate() => new()
    {
        Name = "Diseño de cejas",
        Description = "Diseño personalizado con medición y depilación.",
        DurationMinutes = 45,
        BasePrice = 25.00m,
        CategoryId = 1,
    };

    private static UpdateServiceRequest ValidUpdate() => new()
    {
        Name = "Diseño de cejas",
        DurationMinutes = 45,
        BasePrice = 25.00m,
    };

    // ── Alta ──────────────────────────────────────────────────────────────

    [Fact]
    public void Un_alta_valida_supera_la_validacion()
    {
        new CreateServiceRequestValidator().Validate(ValidCreate()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Un_servicio_sin_categoria_es_valido()
    {
        // La categoría es opcional: un servicio sin clasificar sigue siendo válido.
        var sinCategoria = new CreateServiceRequest
        {
            Name = "Diseño de cejas",
            DurationMinutes = 45,
            BasePrice = 25.00m,
            CategoryId = null,
        };

        new CreateServiceRequestValidator().Validate(sinCategoria).IsValid.Should().BeTrue();
    }

    [Fact]
    public void El_nombre_es_obligatorio()
    {
        var request = new CreateServiceRequest
        {
            Name = "   ",
            DurationMinutes = 45,
            BasePrice = 25.00m,
        };

        new CreateServiceRequestValidator().Validate(request).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateServiceRequest.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void Una_duracion_no_positiva_se_rechaza(int duration)
    {
        // Coincide con el CHECK del esquema: con 0 el servicio no ocuparía hueco
        // en la agenda y la cita no tendría hora de fin.
        var request = new CreateServiceRequest
        {
            Name = "Diseño de cejas",
            DurationMinutes = duration,
            BasePrice = 25.00m,
        };

        new CreateServiceRequestValidator().Validate(request).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateServiceRequest.DurationMinutes));
    }

    [Fact]
    public void Un_precio_negativo_se_rechaza_pero_el_cero_no()
    {
        var gratuito = new CreateServiceRequest
        {
            Name = "Consulta previa",
            DurationMinutes = 15,
            BasePrice = 0m,
        };

        var negativo = new CreateServiceRequest
        {
            Name = "Consulta previa",
            DurationMinutes = 15,
            BasePrice = -1m,
        };

        // Un servicio gratuito es legítimo (una consulta de valoración);
        // uno negativo restaría del importe de la cita.
        new CreateServiceRequestValidator().Validate(gratuito).IsValid.Should().BeTrue();
        new CreateServiceRequestValidator().Validate(negativo).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateServiceRequest.BasePrice));
    }

    [Fact]
    public void La_antelacion_de_la_prueba_de_alergia_solo_se_exige_si_el_servicio_la_pide()
    {
        // Sin prueba, el valor es irrelevante y no debe bloquear el alta.
        var sinPrueba = new CreateServiceRequest
        {
            Name = "Diseño de cejas",
            DurationMinutes = 45,
            BasePrice = 25.00m,
            RequiresAllergyTest = false,
            AllergyTestHoursBefore = 0,
        };

        var conPrueba = new CreateServiceRequest
        {
            Name = "Tinte de cejas",
            DurationMinutes = 30,
            BasePrice = 18.00m,
            RequiresAllergyTest = true,
            AllergyTestHoursBefore = 0,
        };

        new CreateServiceRequestValidator().Validate(sinPrueba).IsValid.Should().BeTrue();
        new CreateServiceRequestValidator().Validate(conPrueba).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateServiceRequest.AllergyTestHoursBefore));
    }

    // ── Edición ───────────────────────────────────────────────────────────

    [Fact]
    public void Una_edicion_valida_supera_la_validacion()
    {
        new UpdateServiceRequestValidator().Validate(ValidUpdate()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void La_edicion_no_puede_dejar_el_servicio_como_el_alta_no_lo_aceptaria()
    {
        // Misma regla que el alta: si no, editar sería la puerta trasera para
        // guardar un servicio de duración 0.
        var request = new UpdateServiceRequest
        {
            Name = "Diseño de cejas",
            DurationMinutes = 0,
            BasePrice = 25.00m,
        };

        new UpdateServiceRequestValidator().Validate(request).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(UpdateServiceRequest.DurationMinutes));
    }
}
