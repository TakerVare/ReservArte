using FluentAssertions;
using ReservArte.Application.DTOs.Services;
using ReservArte.Application.Validators.Services;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Validación de entrada de paquetes (RA-869d7f45n).
///
/// Lo que NO se comprueba aquí, a propósito: que cada `serviceId` exista en el
/// centro. Eso exige mirar la base de datos y lo resuelve `ServicePackageService`.
/// </summary>
public class ServicePackageValidatorTests
{
    private static CreateServicePackageRequest ValidCreate(
        IReadOnlyList<ServicePackageItemRequest>? items = null) => new()
        {
            Name = "Pack cejas completo",
            Description = "Diseño y tinte.",
            TotalPrice = 38.00m,
            DiscountPercentage = 11.50m,
            Items = items ??
        [
            new ServicePackageItemRequest { ServiceId = 1, Order = 0 },
            new ServicePackageItemRequest { ServiceId = 2, Order = 1 },
        ],
        };

    [Fact]
    public void Un_alta_valida_supera_la_validacion()
    {
        new CreateServicePackageRequestValidator().Validate(ValidCreate()).IsValid
            .Should().BeTrue();
    }

    [Fact]
    public void Un_paquete_sin_servicios_se_rechaza()
    {
        // Un paquete sin servicios no es un paquete.
        var result = new CreateServicePackageRequestValidator()
            .Validate(ValidCreate(items: []));

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("al menos un servicio");
    }

    [Fact]
    public void Sin_lista_de_servicios_falla_sin_excepcion()
    {
        var request = new CreateServicePackageRequest
        {
            Name = "Pack",
            TotalPrice = 10m,
            Items = null!,
        };

        // Cascade.Stop: la comprobación de nulo corta antes de contar elementos.
        var act = () => new CreateServicePackageRequestValidator().Validate(request);

        act.Should().NotThrow();
        act().IsValid.Should().BeFalse();
    }

    [Fact]
    public void Un_servicio_repetido_se_rechaza()
    {
        // Repetirlo haría ambiguo su orden dentro del paquete.
        var result = new CreateServicePackageRequestValidator().Validate(ValidCreate(items:
        [
            new ServicePackageItemRequest { ServiceId = 1, Order = 0 },
            new ServicePackageItemRequest { ServiceId = 1, Order = 1 },
        ]));

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("no puede repetirse");
    }

    [Fact]
    public void Un_orden_negativo_en_una_linea_se_rechaza()
    {
        var result = new CreateServicePackageRequestValidator().Validate(ValidCreate(items:
        [
            new ServicePackageItemRequest { ServiceId = 1, Order = -1 },
        ]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("orden no puede ser negativo"));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Un_descuento_fuera_de_0_100_se_rechaza(decimal discount)
    {
        // Coincide con CK_ServicePackages_DiscountPercentage: es un tanto por ciento.
        var request = new CreateServicePackageRequest
        {
            Name = "Pack",
            TotalPrice = 10m,
            DiscountPercentage = discount,
            Items = [new ServicePackageItemRequest { ServiceId = 1, Order = 0 }],
        };

        new CreateServicePackageRequestValidator().Validate(request).IsValid
            .Should().BeFalse();
    }

    [Fact]
    public void Un_paquete_gratuito_es_valido_pero_uno_de_precio_negativo_no()
    {
        var gratuito = new CreateServicePackageRequest
        {
            Name = "Pack bienvenida",
            TotalPrice = 0m,
            Items = [new ServicePackageItemRequest { ServiceId = 1, Order = 0 }],
        };

        var negativo = new CreateServicePackageRequest
        {
            Name = "Pack imposible",
            TotalPrice = -1m,
            Items = [new ServicePackageItemRequest { ServiceId = 1, Order = 0 }],
        };

        new CreateServicePackageRequestValidator().Validate(gratuito).IsValid.Should().BeTrue();
        new CreateServicePackageRequestValidator().Validate(negativo).IsValid.Should().BeFalse();
    }

    [Fact]
    public void La_edicion_aplica_las_mismas_reglas_que_el_alta()
    {
        // Si no, editar sería la puerta trasera para dejar un paquete vacío.
        var request = new UpdateServicePackageRequest
        {
            Name = "Pack cejas",
            TotalPrice = 10m,
            Items = [],
        };

        new UpdateServicePackageRequestValidator().Validate(request).Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("al menos un servicio");
    }
}
