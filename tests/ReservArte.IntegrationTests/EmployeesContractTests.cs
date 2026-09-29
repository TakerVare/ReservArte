using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Domain.Entities;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Contrato HTTP de <c>/api/v1/employees</c> (RA-869f2gh37). Solo la gerencia
/// (Admin y Manager) entra; dentro, un Manager no puede asignar el rol Admin ni
/// gestionar a un administrador, y nadie puede cambiarse el rol ni darse de baja
/// a sí mismo.
/// </summary>
[Collection(ApiCollection.Name)]
public class EmployeesContractTests(ApiFactory factory)
{
    private const string Employees = "/api/v1/employees";

    [Theory]
    [InlineData(Roles.Employee)]
    [InlineData(Roles.Customer)]
    public async Task Fuera_de_la_gerencia_nadie_entra_en_empleados(string rol)
    {
        var token = rol == Roles.Customer
            ? await factory.TokenForAsync(TestData.OrgA, (await factory.CreateCustomerAsync(TestData.OrgA)).Id)
            : (await AccountAsync(rol)).Token;

        var list = await factory.SendAsync(HttpMethod.Get, Employees, TestData.OrgA, token);
        var create = await factory.SendAsync(HttpMethod.Post, Employees, TestData.OrgA, token, NewEmployee(Roles.Employee));

        foreach (var result in new[] { list, create })
        {
            result.Status.Should().Be(HttpStatusCode.Forbidden);
            result.ShouldBeEnvelope(success: false);
            result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
        }
    }

    [Fact]
    public async Task Sin_token_da_401_con_envelope()
    {
        var result = await factory.SendAsync(HttpMethod.Get, Employees, TestData.OrgA, token: null);

        result.Status.Should().Be(HttpStatusCode.Unauthorized);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenUnauthorized);
    }

    [Fact]
    public async Task Un_Manager_da_de_alta_empleadas_con_201_y_Location()
    {
        var (_, token) = await AccountAsync(Roles.Manager);

        var result = await factory.SendAsync(HttpMethod.Post, Employees, TestData.OrgA, token, NewEmployee(Roles.Employee));

        result.Status.Should().Be(HttpStatusCode.Created);
        result.ShouldBeEnvelope(success: true);
        var id = result.Data.GetProperty("id").GetInt32();
        result.Headers.Location!.AbsolutePath.Should().Be($"{Employees}/{id}");
        result.Data.GetProperty("rol").GetString().Should().Be(Roles.Employee);
        factory.Emails.Sent.Should().Contain(m => m.To == result.Data.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Un_Manager_no_puede_crear_un_Admin_y_un_Admin_si()
    {
        var (_, manager) = await AccountAsync(Roles.Manager);
        var (_, admin) = await AccountAsync(Roles.Admin);

        var byManager = await factory.SendAsync(HttpMethod.Post, Employees, TestData.OrgA, manager, NewEmployee(Roles.Admin));
        var byAdmin = await factory.SendAsync(HttpMethod.Post, Employees, TestData.OrgA, admin, NewEmployee(Roles.Admin));

        byManager.Status.Should().Be(HttpStatusCode.Forbidden);
        byManager.ShouldBeEnvelope(success: false);
        byManager.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
        byAdmin.Status.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Un_Manager_no_puede_editar_dar_de_baja_ni_reactivar_a_un_Admin()
    {
        var (_, manager) = await AccountAsync(Roles.Manager);
        var (target, _) = await AccountAsync(Roles.Admin);

        var responses = new[]
        {
            await factory.SendAsync(HttpMethod.Put, $"{Employees}/{target.Id}", TestData.OrgA, manager,
                UpdateOf(target, Roles.Admin, firstName: "Cambio")),
            await factory.SendAsync(HttpMethod.Delete, $"{Employees}/{target.Id}", TestData.OrgA, manager),
            await factory.SendAsync(HttpMethod.Post, $"{Employees}/{target.Id}/reactivate", TestData.OrgA, manager),
        };

        responses.Should().AllSatisfy(r =>
        {
            r.Status.Should().Be(HttpStatusCode.Forbidden);
            r.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
        });
    }

    [Fact]
    public async Task Un_Manager_no_puede_ascender_a_nadie_a_Admin()
    {
        var (_, manager) = await AccountAsync(Roles.Manager);
        var (target, _) = await AccountAsync(Roles.Employee);

        var result = await factory.SendAsync(HttpMethod.Put, $"{Employees}/{target.Id}", TestData.OrgA, manager,
            UpdateOf(target, Roles.Admin));

        result.Status.Should().Be(HttpStatusCode.Forbidden);
        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
    }

    [Fact]
    public async Task Nadie_puede_cambiarse_el_rol_ni_darse_de_baja_a_si_mismo()
    {
        var (self, token) = await AccountAsync(Roles.Manager);

        var changeRole = await factory.SendAsync(HttpMethod.Put, $"{Employees}/{self.Id}", TestData.OrgA, token,
            UpdateOf(self, Roles.Employee));
        var deactivate = await factory.SendAsync(HttpMethod.Delete, $"{Employees}/{self.Id}", TestData.OrgA, token);

        changeRole.Status.Should().Be(HttpStatusCode.Forbidden);
        deactivate.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Un_Manager_edita_da_de_baja_y_reactiva_a_una_empleada()
    {
        var (_, manager) = await AccountAsync(Roles.Manager);
        var (target, _) = await AccountAsync(Roles.Employee);

        var updated = await factory.SendAsync(HttpMethod.Put, $"{Employees}/{target.Id}", TestData.OrgA, manager,
            UpdateOf(target, Roles.Employee, firstName: "Editada"));
        var deactivated = await factory.SendAsync(HttpMethod.Delete, $"{Employees}/{target.Id}", TestData.OrgA, manager);
        var reactivated = await factory.SendAsync(HttpMethod.Post, $"{Employees}/{target.Id}/reactivate", TestData.OrgA, manager);

        updated.Status.Should().Be(HttpStatusCode.OK);
        updated.ShouldBeEnvelope(success: true);
        updated.Data.GetProperty("firstName").GetString().Should().Be("Editada");
        deactivated.Data.GetProperty("isActive").GetBoolean().Should().BeFalse();
        reactivated.Data.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task La_baja_de_una_empleada_bloquea_su_cuenta_y_la_reactivacion_la_desbloquea()
    {
        // La baja usa el lockout de Identity como interruptor (RA-869f180e5): el login,
        // el refresco, la 2FA y el OAuth lo comprueban. El token de acceso ya emitido
        // sigue valiendo hasta que caduca (60 min), como cualquier JWT.
        var (_, manager) = await AccountAsync(Roles.Manager);
        var (target, _) = await AccountAsync(Roles.Employee);

        (await factory.SendAsync(HttpMethod.Delete, $"{Employees}/{target.Id}", TestData.OrgA, manager))
            .Status.Should().Be(HttpStatusCode.OK);
        var lockedAfterDeactivate = await IsLockedOutAsync(target.Id);
        (await factory.SendAsync(HttpMethod.Post, $"{Employees}/{target.Id}/reactivate", TestData.OrgA, manager))
            .Status.Should().Be(HttpStatusCode.OK);
        var lockedAfterReactivate = await IsLockedOutAsync(target.Id);

        lockedAfterDeactivate.Should().BeTrue();
        lockedAfterReactivate.Should().BeFalse();
    }

    private async Task<bool> IsLockedOutAsync(int userId)
    {
        await using var scope = factory.CreateTenantScope(TestData.OrgA);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        return await users.IsLockedOutAsync((await users.FindByIdAsync(userId.ToString()))!);
    }

    [Fact]
    public async Task Un_alta_invalida_da_400_con_los_campos_en_camelCase()
    {
        var (_, token) = await AccountAsync(Roles.Manager);

        var result = await factory.SendAsync(HttpMethod.Post, Employees, TestData.OrgA, token,
            new { firstName = "", lastName = "", email = "mal", rol = "Jefa" });

        result.Status.Should().Be(HttpStatusCode.BadRequest);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        result.Body.GetProperty("error").GetProperty("details").EnumerateArray()
            .Select(d => d.GetProperty("field").GetString())
            .Should().Contain(["firstName", "lastName", "email", "rol"]);
    }

    [Fact]
    public async Task Una_empleada_que_no_existe_da_404_y_un_email_repetido_409()
    {
        var (_, token) = await AccountAsync(Roles.Manager);
        var (existing, _) = await AccountAsync(Roles.Employee);

        var missing = await factory.SendAsync(HttpMethod.Get, $"{Employees}/999999", TestData.OrgA, token);
        var duplicated = await factory.SendAsync(HttpMethod.Post, Employees, TestData.OrgA, token,
            new { firstName = "Otra", lastName = "Prueba", email = existing.Email.ToUpperInvariant(), rol = Roles.Employee });

        missing.Status.Should().Be(HttpStatusCode.NotFound);
        missing.ShouldBeEnvelope(success: false);
        missing.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
        duplicated.Status.Should().Be(HttpStatusCode.Conflict);
        duplicated.ShouldBeEnvelope(success: false);
        duplicated.ErrorCode.Should().Be(ErrorCodes.GenConflict);
    }

    /// <summary>Cuenta nueva del centro A con ficha de empleada, ese rol y su token.</summary>
    private async Task<(Employee Employee, string Token)> AccountAsync(string rol)
    {
        var employee = await factory.CreateEmployeeAsync(TestData.OrgA, rol);
        return (employee, await factory.TokenForAsync(TestData.OrgA, employee.Id));
    }

    private static object NewEmployee(string rol) => new
    {
        firstName = "Nueva",
        lastName = "Contrato",
        email = TestData.UniqueEmail("empleada.contrato"),
        rol,
    };

    private static object UpdateOf(Employee employee, string rol, string firstName = "Empleada") => new
    {
        firstName,
        lastName = employee.LastName,
        email = employee.Email,
        rol,
    };
}
