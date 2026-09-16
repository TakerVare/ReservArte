using FluentAssertions;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Contrato de dominio del módulo de Servicios (RA-869d7f3wa).
///
/// Mismo criterio que <see cref="CustomerDomainTests"/>: los catálogos se
/// persisten como texto con un CHECK en el esquema, y un valor que no coincida
/// no falla al compilar. Estos tests fijan los valores y el tipo del tenant
/// antes de que exista la migración.
/// </summary>
public class ServiceDomainTests
{
    [Fact]
    public void Los_niveles_con_tarifa_propia_son_junior_senior_y_expert()
    {
        EmployeeLevels.All.Should().BeEquivalentTo(new[] { "junior", "senior", "expert" });
    }

    [Fact]
    public void Los_valores_de_catalogo_estan_en_snake_case_minusculas()
    {
        // Misma convención que EmployeeExceptionTypes y los catálogos de
        // Clientes. Roles es la excepción (PascalCase) porque lo impone
        // [Authorize(Roles = …)].
        EmployeeLevels.All.Should().AllSatisfy(v => v.Should().MatchRegex("^[a-z]+(_[a-z]+)*$"));
    }

    [Theory]
    [InlineData(typeof(Service))]
    [InlineData(typeof(ServiceCategory))]
    [InlineData(typeof(ServiceVariation))]
    [InlineData(typeof(ServicePricing))]
    [InlineData(typeof(ServicePackage))]
    [InlineData(typeof(ServicePackageItem))]
    [InlineData(typeof(EmployeeServiceAssignment))]
    public void El_tenant_es_un_Guid_como_Organization_Id(Type entity)
    {
        // Todas nacieron con OrganizationId int, incompatible con
        // Organization.Id (Guid): la FK no se habría podido mapear. Mismo
        // arreglo que en Customer (RA-869d7f2z5).
        entity.GetProperty("OrganizationId")!.PropertyType.Should().Be(typeof(Guid));
    }

    [Theory]
    [InlineData(typeof(ServiceVariation))]
    [InlineData(typeof(ServicePricing))]
    [InlineData(typeof(ServicePackageItem))]
    [InlineData(typeof(EmployeeServiceAssignment))]
    public void Las_entidades_hijas_llevan_el_tenant_propio(Type entity)
    {
        // Redundante con el padre a propósito (RA-869f17myx): el query filter
        // global no puede depender de un JOIN, o una consulta directa a la
        // tabla cruzaría organizaciones. El test de metadatos de
        // TenantQueryFilterTests exigirá el filtro en cuanto se mapeen.
        entity.GetProperty("OrganizationId").Should().NotBeNull();
        entity.GetProperty("Organization").Should().NotBeNull("el filtro necesita la navegación");
    }

    [Fact]
    public void Un_servicio_nuevo_nace_activo_y_sin_exigir_prueba_de_alergia()
    {
        var service = new Service();

        service.IsActive.Should().BeTrue();
        service.RequiresAllergyTest.Should().BeFalse();

        // La antelación del producto (vol. 1 §3.1.4) vale aunque nadie la fije;
        // un 0 por defecto dejaría pasar una prueba de alergia el mismo día.
        service.AllergyTestHoursBefore.Should().Be(48);
    }

    [Fact]
    public void Una_variacion_nueva_no_altera_el_precio_ni_la_duracion_del_servicio()
    {
        // Son modificadores, no valores absolutos: los de una variante recién
        // creada deben ser neutros.
        var variation = new ServiceVariation();

        variation.PriceModifier.Should().Be(0);
        variation.DurationModifier.Should().Be(0);
        variation.IsActive.Should().BeTrue();
    }

    [Fact]
    public void La_destreza_en_un_servicio_nace_en_el_minimo_del_CHECK()
    {
        // El esquema restringe ProficiencyLevel a 1-5: un 0 por defecto haría
        // fallar el alta de una asignación que no lo indique.
        new EmployeeServiceAssignment().ProficiencyLevel.Should().Be(1);
    }

    [Theory]
    [InlineData(typeof(Service), "Products")]
    [InlineData(typeof(Service), "Promotions")]
    [InlineData(typeof(Service), "WaitingLists")]
    [InlineData(typeof(ServiceVariation), "AppointmentItems")]
    [InlineData(typeof(ServicePackage), "Promotions")]
    public void El_catalogo_no_navega_a_modulos_que_aun_no_existen(Type entity, string navigation)
    {
        // Inventario, promociones, lista de espera y citas siguen en Ignore.
        // EF descarta las navegaciones hacia tipos ignorados, así que dejarlas
        // no rompe el modelo; se retiran por el mismo criterio que en Customer
        // y Employee: la navegación llega con su módulo.
        entity.GetProperty(navigation).Should().BeNull();
    }
}
