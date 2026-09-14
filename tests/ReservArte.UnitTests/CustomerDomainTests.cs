using FluentAssertions;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Contrato de dominio del módulo de Clientes (RA-869d7f2z5).
///
/// Los catálogos se persistirán como texto con un CHECK en el esquema, y un
/// valor que no coincida con él no falla al compilar: falla al guardar, o peor,
/// deja de coincidir en silencio en un filtro. Estos tests fijan los valores.
/// </summary>
public class CustomerDomainTests
{
    [Fact]
    public void Las_categorias_son_regular_vip_y_new_sin_bloqueado()
    {
        // El bloqueo es Customer.IsBlocked: como categoría duplicaría el dato y
        // haría perder el segmento del cliente al desbloquearlo.
        CustomerCategories.All.Should().BeEquivalentTo(new[] { "regular", "vip", "new" });
    }

    [Fact]
    public void Los_metodos_de_contacto_son_los_del_producto()
    {
        CustomerContactMethods.All.Should().BeEquivalentTo(new[] { "email", "phone", "sms", "whatsapp" });
    }

    [Fact]
    public void Las_severidades_de_alergia_son_low_medium_y_high()
    {
        AllergySeverities.All.Should().BeEquivalentTo(new[] { "low", "medium", "high" });
    }

    [Fact]
    public void Los_tipos_de_consentimiento_cubren_las_finalidades_del_RGPD()
    {
        CustomerConsentTypes.All.Should().BeEquivalentTo(
            new[] { "data_processing", "marketing", "photos", "whatsapp", "saved_cards" });
    }

    [Fact]
    public void Solo_el_tratamiento_de_datos_es_obligatorio()
    {
        // Marcar como obligatoria una finalidad opcional (marketing, fotos…)
        // convertiría el alta en un consentimiento forzado.
        CustomerConsentTypes.Required.Should().BeEquivalentTo(new[] { CustomerConsentTypes.DataProcessing });
        CustomerConsentTypes.All.Should().Contain(CustomerConsentTypes.Required);
    }

    [Fact]
    public void Todos_los_valores_de_catalogo_estan_en_snake_case_minusculas()
    {
        // Misma convención que EmployeeExceptionTypes. Roles es la excepción
        // (PascalCase) porque lo impone [Authorize(Roles = …)].
        var values = CustomerCategories.All
            .Concat(CustomerContactMethods.All)
            .Concat(AllergySeverities.All)
            .Concat(CustomerConsentTypes.All);

        values.Should().AllSatisfy(v => v.Should().MatchRegex("^[a-z]+(_[a-z]+)*$"));
    }

    [Fact]
    public void Un_cliente_nuevo_nace_con_valores_del_catalogo()
    {
        var customer = new Customer();

        CustomerCategories.All.Should().Contain(customer.Category);
        CustomerContactMethods.All.Should().Contain(customer.PreferredContactMethod);
        customer.IsActive.Should().BeTrue();
        customer.IsBlocked.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(Customer))]
    [InlineData(typeof(CustomerNote))]
    [InlineData(typeof(CustomerAllergy))]
    [InlineData(typeof(CustomerConsent))]
    public void El_tenant_es_un_Guid_como_Organization_Id(Type entity)
    {
        // Customer nació con OrganizationId int, incompatible con Organization.Id
        // (Guid): la FK no se habría podido mapear.
        entity.GetProperty("OrganizationId")!.PropertyType.Should().Be(typeof(Guid));
    }

    [Fact]
    public void El_cliente_no_duplica_el_rol_ni_el_consentimiento_de_marketing()
    {
        // El rol vive en User.Rol (una copia quedaría desfasada si la cuenta se
        // eleva a personal) y el marketing en CustomerConsents.
        typeof(Customer).GetProperty("Rol").Should().BeNull();
        typeof(Customer).GetProperty("MarketingConsent").Should().BeNull();
    }
}
