using FluentAssertions;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Application.Validators.Customers;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>Validación de entrada del alta y la edición de clientes (RA-869d7f369).</summary>
public class CustomerValidatorTests
{
    private static CreateCustomerRequest ValidCreate(
        string[]? consents = null, string? category = null, string contact = CustomerContactMethods.Email) => new()
    {
        FirstName = "Lucía",
        LastName = "Martínez",
        Email = "lucia@correo.com",
        Phone = "+34 600 111 222",
        BirthDate = new DateOnly(1990, 5, 20),
        Category = category,
        PreferredContactMethod = contact,
        GrantedConsents = consents ?? new[] { CustomerConsentTypes.DataProcessing },
    };

    private static UpdateCustomerRequest ValidUpdate(string category = CustomerCategories.Regular) => new()
    {
        FirstName = "Lucía",
        LastName = "Martínez",
        Email = "lucia@correo.com",
        Category = category,
        PreferredContactMethod = CustomerContactMethods.Sms,
    };

    // ── Alta ──────────────────────────────────────────────────────────────

    [Fact]
    public void Un_alta_valida_sin_categoria_supera_la_validacion()
    {
        new CreateCustomerRequestValidator().Validate(ValidCreate()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Sin_tratamiento_de_datos_el_alta_no_es_valida()
    {
        var result = new CreateCustomerRequestValidator().Validate(
            ValidCreate(consents: new[] { CustomerConsentTypes.Marketing }));

        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateCustomerRequest.GrantedConsents));
    }

    [Fact]
    public void Sin_lista_de_consentimientos_falla_sin_excepcion()
    {
        var request = new CreateCustomerRequest
        {
            FirstName = "Lucía",
            LastName = "Martínez",
            Email = "lucia@correo.com",
            GrantedConsents = null!,
        };

        new CreateCustomerRequestValidator().Validate(request).Errors
            .Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateCustomerRequest.GrantedConsents));
    }

    [Fact]
    public void Un_consentimiento_fuera_del_catalogo_no_es_valido()
    {
        var result = new CreateCustomerRequestValidator().Validate(
            ValidCreate(consents: new[] { CustomerConsentTypes.DataProcessing, "newsletter" }));

        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().StartWith(nameof(CreateCustomerRequest.GrantedConsents));
    }

    [Fact]
    public void Un_consentimiento_repetido_no_es_valido()
    {
        var result = new CreateCustomerRequestValidator().Validate(
            ValidCreate(consents: new[] { CustomerConsentTypes.DataProcessing, CustomerConsentTypes.DataProcessing }));

        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be(nameof(CreateCustomerRequest.GrantedConsents));
    }

    [Fact]
    public void Categoria_y_canal_de_contacto_deben_ser_del_catalogo()
    {
        var result = new CreateCustomerRequestValidator().Validate(
            ValidCreate(category: "blocked", contact: "fax"));

        result.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(
            new[] { nameof(CreateCustomerRequest.Category), nameof(CreateCustomerRequest.PreferredContactMethod) });
    }

    [Fact]
    public void Email_y_fecha_de_nacimiento_se_validan()
    {
        var request = new CreateCustomerRequest
        {
            FirstName = "Lucía",
            LastName = "Martínez",
            Email = "no-es-un-email",
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
            GrantedConsents = new[] { CustomerConsentTypes.DataProcessing },
        };

        new CreateCustomerRequestValidator().Validate(request).Errors
            .Select(e => e.PropertyName).Should().BeEquivalentTo(
                new[] { nameof(CreateCustomerRequest.Email), nameof(CreateCustomerRequest.BirthDate) });
    }

    // ── Edición ───────────────────────────────────────────────────────────

    [Fact]
    public void Una_edicion_valida_supera_la_validacion()
    {
        new UpdateCustomerRequestValidator().Validate(ValidUpdate()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("blocked")]
    public void La_edicion_exige_una_categoria_del_catalogo(string category)
    {
        new UpdateCustomerRequestValidator().Validate(ValidUpdate(category)).Errors
            .Should().NotBeEmpty()
            .And.OnlyContain(e => e.PropertyName == nameof(UpdateCustomerRequest.Category));
    }
}
