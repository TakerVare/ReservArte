using FluentAssertions;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Catálogo canónico de roles (RA-869f18116).
///
/// Estos tests parecen triviales y no lo son: el casing de estos valores viaja
/// al claim `role` del JWT y se compara en `[Authorize(Roles = …)]`, donde un
/// desajuste **no da error de compilación, deniega el acceso en silencio**.
/// Fijarlos aquí convierte ese fallo mudo en un test rojo.
/// </summary>
public class RolesTests
{
    [Fact]
    public void El_catalogo_tiene_exactamente_los_cuatro_roles_del_producto()
    {
        Roles.All.Should().BeEquivalentTo(new[] { "Admin", "Manager", "Employee", "Customer" });
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("Employee")]
    [InlineData("Customer")]
    public void Los_roles_estan_en_PascalCase(string rol)
    {
        // Si alguien "normaliza" a minúsculas, el primer [Authorize(Roles=…)]
        // dejaría fuera a todo el mundo sin avisar.
        rol.Should().MatchRegex("^[A-Z][a-z]+$");
        Roles.All.Should().Contain(rol);
    }

    [Fact]
    public void Un_cliente_no_puede_asignarse_a_una_ficha_de_empleado()
    {
        Roles.AssignableToEmployee.Should().NotContain(Roles.Customer);
        Roles.AssignableToEmployee.Should()
            .BeEquivalentTo(new[] { Roles.Admin, Roles.Manager, Roles.Employee });
    }

    [Fact]
    public void El_rol_por_defecto_de_los_DTO_sale_del_catalogo_y_no_de_un_literal()
    {
        // Si el default fuese un literal suelto, cambiar la constante dejaría
        // los DTO con un valor huérfano que el validador rechazaría.
        new CreateEmployeeRequest().Rol.Should().Be(Roles.Employee);
        new UpdateEmployeeRequest().Rol.Should().Be(Roles.Employee);
        Roles.AssignableToEmployee.Should().Contain(new CreateEmployeeRequest().Rol);
    }

    [Fact]
    public void El_registro_publico_crea_clientes_y_no_personal_del_centro()
    {
        // Nacer como Employee abriría el backoffice a cualquiera que se
        // registrase en la web pública en cuanto existan los [Authorize].
        Roles.DefaultForPublicRegistration.Should().Be(Roles.Customer);
        Roles.AssignableToEmployee.Should().NotContain(Roles.DefaultForPublicRegistration);
    }
}
