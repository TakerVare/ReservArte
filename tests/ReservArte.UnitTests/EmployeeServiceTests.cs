using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Application.Mapping;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Services;
using ReservArte.Shared.Api;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Casos de uso del módulo de Empleados (RA-869d7ezwy). El repositorio y
/// Identity van dobles: aquí se prueban las reglas del servicio, no el acceso
/// a datos (que cubre EmployeeRepositoryTests contra SQLite real).
/// </summary>
public class EmployeeServiceTests
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly Mock<UserManager<User>> _userManager = CreateUserManagerMock();
    private readonly IMapper _mapper = CreateMapper();

    private sealed class FakeCurrentOrganization : ICurrentOrganizationService
    {
        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();

        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            cfg => cfg.AddProfile<EmployeeProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

    private EmployeeService CreateService(Guid? organizationId = null)
    {
        var tenant = new FakeCurrentOrganization();
        if ((organizationId ?? OrgA) is { } id && organizationId is not null)
        {
            tenant.SetOrganization(id);
        }

        return new EmployeeService(
            _repository.Object,
            _userManager.Object,
            tenant,
            _mapper,
            NullLogger<EmployeeService>.Instance);
    }

    private static Employee ExistingEmployee(bool isActive = true) => new()
    {
        Id = 7,
        OrganizationId = OrgA,
        FirstName = "María",
        LastName = "Salas",
        Email = "maria@reservarte.com",
        Rol = "employee",
        IsActive = isActive,
    };

    // ── Alta ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_sin_tenant_resuelto_falla_sin_tocar_la_base()
    {
        var service = CreateService(organizationId: null);

        var result = await service.CreateAsync(new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.OrgTenantNotResolved);
        _repository.Verify(r => r.Add(It.IsAny<Employee>()), Times.Never);
        _userManager.Verify(m => m.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_con_email_ya_usado_devuelve_conflicto()
    {
        _repository
            .Setup(r => r.EmailExistsAsync("maria@reservarte.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(OrgA);

        var result = await service.CreateAsync(new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenConflict);

        // No debe llegar a crear el usuario de Identity.
        _userManager.Verify(m => m.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_crea_el_usuario_sin_contrasena_y_la_ficha_con_su_id()
    {
        _repository
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        User? creado = null;
        _userManager
            .Setup(m => m.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => { u.Id = 42; creado = u; })
            .ReturnsAsync(IdentityResult.Success);

        Employee? guardado = null;
        _repository.Setup(r => r.Add(It.IsAny<Employee>()))
            .Callback<Employee>(e => guardado = e);

        var service = CreateService(OrgA);

        var result = await service.CreateAsync(new CreateEmployeeRequest
        {
            FirstName = "  María  ",
            LastName = "Salas",
            Email = " maria@reservarte.com ",
            Rol = "admin",
        });

        result.Success.Should().BeTrue();

        // La cuenta se crea SIN contraseña: el empleado la establecerá con el
        // flujo de recuperación.
        _userManager.Verify(m => m.CreateAsync(It.IsAny<User>()), Times.Once);
        _userManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);

        creado!.OrganizationId.Should().Be(OrgA);
        creado.Email.Should().Be("maria@reservarte.com", "el email se normaliza sin espacios");

        // Clave primaria compartida: la ficha hereda el Id del usuario.
        guardado!.Id.Should().Be(42);
        guardado.OrganizationId.Should().Be(OrgA);
        guardado.FirstName.Should().Be("María");
        guardado.IsActive.Should().BeTrue();
        result.Data!.FullName.Should().Be("María Salas");
    }

    [Fact]
    public async Task CreateAsync_revierte_el_usuario_si_falla_el_guardado_de_la_ficha()
    {
        _repository
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userManager
            .Setup(m => m.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => u.Id = 42)
            .ReturnsAsync(IdentityResult.Success);

        _repository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo al guardar"));

        _userManager
            .Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = CreateService(OrgA);

        var act = () => service.CreateAsync(new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
        });

        await act.Should().ThrowAsync<InvalidOperationException>();

        // Sin esto, el usuario quedaría huérfano bloqueando el email para siempre.
        _userManager.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_traduce_el_email_duplicado_de_Identity_a_conflicto()
    {
        _repository
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userManager
            .Setup(m => m.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail" }));

        var service = CreateService(OrgA);

        var result = await service.CreateAsync(new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
        });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenConflict);
        _repository.Verify(r => r.Add(It.IsAny<Employee>()), Times.Never);
    }

    // ── Consulta ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_devuelve_no_encontrado_cuando_no_existe()
    {
        _repository
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var result = await CreateService(OrgA).GetByIdAsync(99);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    [Fact]
    public async Task GetPagedAsync_mapea_los_elementos_conservando_el_recuento()
    {
        _repository
            .Setup(r => r.GetPagedAsync(It.IsAny<EmployeeFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Employee>
            {
                Items = new[] { ExistingEmployee() },
                TotalCount = 37,
                Page = 2,
                PageSize = 20,
            });

        var result = await CreateService(OrgA).GetPagedAsync(new EmployeeFilter());

        result.Success.Should().BeTrue();
        result.Data!.TotalCount.Should().Be(37);
        result.Data.Page.Should().Be(2);
        result.Data.Items.Should().ContainSingle()
            .Which.FullName.Should().Be("María Salas");
    }

    // ── Baja lógica ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_desactiva_y_no_borra()
    {
        var employee = ExistingEmployee();
        _repository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var result = await CreateService(OrgA).DeactivateAsync(7);

        result.Success.Should().BeTrue();
        employee.IsActive.Should().BeFalse();
        _repository.Verify(r => r.Update(employee), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_sobre_un_empleado_ya_de_baja_no_vuelve_a_guardar()
    {
        _repository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingEmployee(isActive: false));

        var result = await CreateService(OrgA).DeactivateAsync(7);

        result.Success.Should().BeTrue("la operación es idempotente");
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReactivateAsync_vuelve_a_dar_de_alta_al_empleado()
    {
        var employee = ExistingEmployee(isActive: false);
        _repository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var result = await CreateService(OrgA).ReactivateAsync(7);

        result.Success.Should().BeTrue();
        employee.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_de_un_empleado_inexistente_devuelve_no_encontrado()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var result = await CreateService(OrgA).DeactivateAsync(123);

        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    // ── Edición ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_propaga_email_y_rol_a_la_cuenta_de_acceso()
    {
        var employee = ExistingEmployee();
        var user = new User { Id = 7, Email = "maria@reservarte.com", Rol = "employee" };

        _repository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _repository
            .Setup(r => r.EmailExistsAsync(It.IsAny<string>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync(user);
        _userManager.Setup(m => m.SetEmailAsync(user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetUserNameAsync(user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await CreateService(OrgA).UpdateAsync(7, new UpdateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "nuevo@reservarte.com",
            Rol = "admin",
        });

        result.Success.Should().BeTrue();
        employee.Email.Should().Be("nuevo@reservarte.com");
        employee.Rol.Should().Be("admin");

        // Si la cuenta no siguiera a la ficha, el empleado entraría con datos
        // obsoletos y el rol del JWT quedaría desfasado.
        user.Rol.Should().Be("admin");
        _userManager.Verify(m => m.SetEmailAsync(user, "nuevo@reservarte.com"), Times.Once);
        _userManager.Verify(m => m.SetUserNameAsync(user, "nuevo@reservarte.com"), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_con_un_email_de_otro_empleado_devuelve_conflicto()
    {
        _repository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingEmployee());
        _repository
            .Setup(r => r.EmailExistsAsync("ocupado@reservarte.com", 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateService(OrgA).UpdateAsync(7, new UpdateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "ocupado@reservarte.com",
        });

        result.ErrorCode.Should().Be(ErrorCodes.GenConflict);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task DeactivateAsync_bloquea_ademas_la_cuenta_de_acceso()
    {
        var employee = ExistingEmployee();
        var user = new User { Id = 7, Email = "maria@reservarte.com" };

        _repository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _userManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync(user);
        _userManager.Setup(m => m.SetLockoutEnabledAsync(user, true))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(IdentityResult.Success);

        await CreateService(OrgA).DeactivateAsync(7);

        // Sin esto, el empleado de baja seguiría entrando: AuthService valida
        // contra AspNetUsers y no mira Employee.IsActive (RA-869f180e5).
        _userManager.Verify(m => m.SetLockoutEnabledAsync(user, true), Times.Once);
        _userManager.Verify(
            m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
    }

    [Fact]
    public async Task ReactivateAsync_retira_el_bloqueo_de_la_cuenta()
    {
        var employee = ExistingEmployee(isActive: false);
        var user = new User { Id = 7, Email = "maria@reservarte.com" };

        _repository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _userManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync(user);
        _userManager.Setup(m => m.SetLockoutEnabledAsync(user, true))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset?>()))
            .ReturnsAsync(IdentityResult.Success);

        await CreateService(OrgA).ReactivateAsync(7);

        _userManager.Verify(m => m.SetLockoutEndDateAsync(user, null), Times.Once);
    }

    [Fact]
    public async Task Una_desactivacion_idempotente_no_vuelve_a_tocar_la_cuenta()
    {
        _repository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingEmployee(isActive: false));

        await CreateService(OrgA).DeactivateAsync(7);

        _userManager.Verify(
            m => m.SetLockoutEndDateAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset?>()),
            Times.Never);
    }

    [Fact]
    public async Task Si_el_empleado_no_tiene_cuenta_la_baja_no_revienta()
    {
        var employee = ExistingEmployee();
        _repository
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _userManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync((User?)null);

        var result = await CreateService(OrgA).DeactivateAsync(7);

        result.Success.Should().BeTrue();
        employee.IsActive.Should().BeFalse();
    }
}
