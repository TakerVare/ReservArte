using FluentAssertions;
using ReservArte.Application.DTOs.Services;
using ReservArte.Application.Validators.Services;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Validación de entrada de las escrituras de categorías, variaciones y tarifas
/// (RA-869f2wtrk).
///
/// Lo que NO se comprueba aquí, a propósito: que el nivel pertenezca a
/// `EmployeeLevels` y que la duración resultante de una variación sea positiva.
/// Lo primero viaja en la ruta y lo segundo depende del servicio al que se
/// añade, así que ambos los resuelve `ServiceCatalogService`.
/// </summary>
public class ServiceCatalogWriteValidatorTests
{
    // ── Categorías ────────────────────────────────────────────────────────

    [Fact]
    public void Un_alta_de_categoria_valida_supera_la_validacion()
    {
        var request = new CreateServiceCategoryRequest
        {
            Name = "Cejas",
            Description = "Diseño y tinte.",
            Color = "#8B5E3C",
            DisplayOrder = 0,
        };

        new CreateServiceCategoryRequestValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Una_categoria_sin_nombre_se_rechaza()
    {
        var request = new CreateServiceCategoryRequest { Name = "   " };

        new CreateServiceCategoryRequestValidator().Validate(request).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateServiceCategoryRequest.Name));
    }

    [Fact]
    public void Un_orden_de_presentacion_negativo_se_rechaza()
    {
        // Es una posición en el listado: un negativo no significa nada.
        var request = new CreateServiceCategoryRequest { Name = "Cejas", DisplayOrder = -1 };

        new CreateServiceCategoryRequestValidator().Validate(request).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateServiceCategoryRequest.DisplayOrder));
    }

    [Fact]
    public void La_edicion_de_categoria_aplica_las_mismas_reglas_que_el_alta()
    {
        // Si no, editar sería la puerta trasera para dejar una categoría sin nombre.
        var request = new UpdateServiceCategoryRequest { Name = string.Empty, DisplayOrder = 0 };

        new UpdateServiceCategoryRequestValidator().Validate(request).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(UpdateServiceCategoryRequest.Name));
    }

    // ── Variaciones ───────────────────────────────────────────────────────

    [Fact]
    public void Una_variacion_valida_supera_la_validacion()
    {
        var request = new CreateServiceVariationRequest
        {
            Name = "Con hilo",
            PriceModifier = 5.00m,
            DurationModifier = 15,
        };

        new CreateServiceVariationRequestValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Los_modificadores_negativos_son_validos()
    {
        // Una variante puede ser más corta y más barata que el servicio base;
        // que la duración resultante siga siendo positiva lo comprueba el servicio.
        var request = new CreateServiceVariationRequest
        {
            Name = "Retoque",
            PriceModifier = -5.00m,
            DurationModifier = -10,
        };

        new CreateServiceVariationRequestValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Una_variacion_sin_nombre_se_rechaza()
    {
        var request = new CreateServiceVariationRequest { Name = string.Empty };

        new CreateServiceVariationRequestValidator().Validate(request).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateServiceVariationRequest.Name));
    }

    // ── Tarifas ───────────────────────────────────────────────────────────

    [Fact]
    public void Una_tarifa_gratuita_es_valida_pero_una_negativa_no()
    {
        var gratuita = new UpsertServicePricingRequest { Price = 0m };
        var negativa = new UpsertServicePricingRequest { Price = -0.01m };

        new UpsertServicePricingRequestValidator().Validate(gratuita).IsValid.Should().BeTrue();
        new UpsertServicePricingRequestValidator().Validate(negativa).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(UpsertServicePricingRequest.Price));
    }
}
