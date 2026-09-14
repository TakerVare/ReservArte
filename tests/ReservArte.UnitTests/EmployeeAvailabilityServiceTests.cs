using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Mapping;
using ReservArte.Infrastructure.Options;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Services;
using ReservArte.Shared.Api;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Casos de uso de disponibilidad (RA-869d7f01b): horario semanal y ausencias.
/// El acceso a datos va doble; el contrato real del repositorio lo cubre
/// EmployeeRepositoryTests contra SQLite.
/// </summary>
public class EmployeeAvailabilityServiceTests
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

    private const int EmployeeId = 7;
    private const int CallerId = 1;

    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly Mock<UserManager<User>> _userManager = CreateUserManagerMock();
    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<EmployeeProfile>(), NullLoggerFactory.Instance).CreateMapper();

    /// <summary>
    /// Sin estos valores por defecto, Moq devolvería null dentro del
    /// `Task&lt;IReadOnlyList&lt;T&gt;&gt;` y cualquier caso que no fije horario
    /// ni ausencias reventaría al construir la respuesta.
    /// </summary>
    public EmployeeAvailabilityServiceTests()
    {
        _repository
            .Setup(r => r.GetAvailabilitiesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmployeeAvailability>());

        _repository
            .Setup(r => r.GetExceptionsAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmployeeException>());
    }

    private sealed class FakeCurrentOrganization : ICurrentOrganizationService
    {
        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public int? UserId { get; init; }

        public string? Role { get; init; }
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();

        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private EmployeeService CreateService(
        bool tenantResolved = true, string? callerRole = Roles.Admin)
    {
        var tenant = new FakeCurrentOrganization();
        if (tenantResolved)
        {
            tenant.SetOrganization(OrgA);
        }

        return new EmployeeService(
            _repository.Object,
            _userManager.Object,
            tenant,
            new FakeCurrentUser { UserId = CallerId, Role = callerRole },
            Mock.Of<IEmailService>(),
            Options.Create(new AppOptions { FrontendBaseUrl = "http://localhost:3000" }),
            _mapper,
            NullLogger<EmployeeService>.Instance);
    }

    /// <summary>Deja el empleado localizable por el repositorio.</summary>
    private Employee GivenEmployee(string rol = Roles.Employee)
    {
        var employee = new Employee
        {
            Id = EmployeeId,
            OrganizationId = OrgA,
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
            Rol = rol,
        };

        _repository
            .Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        return employee;
    }

    private void GivenNoEmployee() =>
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

    private static EmployeeAvailability Availability(int day, string start, string end) => new()
    {
        Id = 1,
        EmployeeId = EmployeeId,
        OrganizationId = OrgA,
        DayOfWeek = day,
        StartTime = TimeOnly.Parse(start),
        EndTime = TimeOnly.Parse(end),
    };

    private static EmployeeException Exception(bool isActive = true) => new()
    {
        Id = 33,
        EmployeeId = EmployeeId,
        OrganizationId = OrgA,
        StartDateTime = new DateTime(2026, 12, 24),
        EndDateTime = new DateTime(2026, 12, 26),
        Type = EmployeeExceptionTypes.Vacation,
        IsActive = isActive,
    };

    // ── Consulta ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailabilityAsync_devuelve_horario_y_ausencias()
    {
        GivenEmployee();
        _repository
            .Setup(r => r.GetAvailabilitiesAsync(EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Availability(WeekDay.Monday, "09:00", "18:00") });
        _repository
            .Setup(r => r.GetExceptionsAsync(
                EmployeeId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Exception() });

        var result = await CreateService().GetAvailabilityAsync(EmployeeId, null, null);

        result.Success.Should().BeTrue();
        result.Data!.EmployeeId.Should().Be(EmployeeId);
        result.Data.WeeklySchedule.Should().ContainSingle()
            .Which.DayOfWeek.Should().Be(WeekDay.Monday);
        result.Data.Exceptions.Should().ContainSingle()
            .Which.Type.Should().Be(EmployeeExceptionTypes.Vacation);
    }

    [Fact]
    public async Task GetAvailabilityAsync_sin_rango_pide_desde_hoy_y_noventa_dias()
    {
        GivenEmployee();

        await CreateService().GetAvailabilityAsync(EmployeeId, null, null);

        var hoy = DateTime.UtcNow.Date;
        _repository.Verify(
            r => r.GetExceptionsAsync(
                EmployeeId, hoy, hoy.AddDays(90), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAvailabilityAsync_respeta_el_rango_pedido_y_lo_devuelve()
    {
        GivenEmployee();
        var from = new DateTime(2027, 1, 1);
        var to = new DateTime(2027, 1, 31);

        var result = await CreateService().GetAvailabilityAsync(EmployeeId, from, to);

        result.Data!.ExceptionsFrom.Should().Be(from);
        result.Data.ExceptionsTo.Should().Be(to);
        _repository.Verify(
            r => r.GetExceptionsAsync(EmployeeId, from, to, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAvailabilityAsync_con_solo_from_completa_los_noventa_dias()
    {
        GivenEmployee();
        var from = new DateTime(2027, 1, 1);

        var result = await CreateService().GetAvailabilityAsync(EmployeeId, from, null);

        result.Data!.ExceptionsTo.Should().Be(from.AddDays(90));
    }

    [Fact]
    public async Task GetAvailabilityAsync_con_el_rango_invertido_falla_la_validacion()
    {
        GivenEmployee();

        var result = await CreateService().GetAvailabilityAsync(
            EmployeeId, new DateTime(2027, 2, 1), new DateTime(2027, 1, 1));

        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        _repository.Verify(
            r => r.GetExceptionsAsync(
                It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAvailabilityAsync_de_un_empleado_inexistente_devuelve_no_encontrado()
    {
        GivenNoEmployee();

        var result = await CreateService().GetAvailabilityAsync(EmployeeId, null, null);

        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    [Fact]
    public async Task Un_manager_si_puede_consultar_la_disponibilidad_de_un_admin()
    {
        GivenEmployee(rol: Roles.Admin);

        var result = await CreateService(callerRole: Roles.Manager)
            .GetAvailabilityAsync(EmployeeId, null, null);

        result.Success.Should().BeTrue("la consulta solo lee; la lista ya muestra a los Admin");
    }

    // ── Horario semanal ───────────────────────────────────────────────────

    [Fact]
    public async Task ReplaceAvailabilityAsync_reemplaza_el_horario_completo()
    {
        GivenEmployee();
        IEnumerable<EmployeeAvailability>? guardados = null;
        _repository
            .Setup(r => r.ReplaceAvailabilitiesAsync(
                EmployeeId, It.IsAny<IEnumerable<EmployeeAvailability>>(), It.IsAny<CancellationToken>()))
            .Callback<int, IEnumerable<EmployeeAvailability>, CancellationToken>(
                (_, slots, _) => guardados = slots)
            .Returns(Task.CompletedTask);

        var result = await CreateService().ReplaceAvailabilityAsync(
            EmployeeId,
            new UpdateAvailabilityRequest
            {
                WeeklySchedule = new[]
                {
                    new AvailabilitySlotRequest
                    {
                        DayOfWeek = WeekDay.Friday,
                        StartTime = new TimeOnly(8, 0),
                        EndTime = new TimeOnly(15, 0),
                    },
                },
            });

        result.Success.Should().BeTrue();
        guardados.Should().ContainSingle();
        guardados!.Single().DayOfWeek.Should().Be(WeekDay.Friday);
        guardados.Single().IsActive.Should().BeTrue();
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceAvailabilityAsync_con_lista_vacia_deja_al_empleado_sin_horario()
    {
        GivenEmployee();

        var result = await CreateService().ReplaceAvailabilityAsync(
            EmployeeId, new UpdateAvailabilityRequest());

        result.Success.Should().BeTrue();
        _repository.Verify(
            r => r.ReplaceAvailabilitiesAsync(
                EmployeeId,
                It.Is<IEnumerable<EmployeeAvailability>>(s => !s.Any()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReplaceAvailabilityAsync_un_manager_no_puede_tocar_el_horario_de_un_admin()
    {
        GivenEmployee(rol: Roles.Admin);

        var result = await CreateService(callerRole: Roles.Manager)
            .ReplaceAvailabilityAsync(EmployeeId, new UpdateAvailabilityRequest());

        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
        _repository.Verify(
            r => r.ReplaceAvailabilitiesAsync(
                It.IsAny<int>(),
                It.IsAny<IEnumerable<EmployeeAvailability>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceAvailabilityAsync_de_un_empleado_inexistente_devuelve_no_encontrado()
    {
        GivenNoEmployee();

        var result = await CreateService().ReplaceAvailabilityAsync(
            EmployeeId, new UpdateAvailabilityRequest());

        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    // ── Ausencias ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AddExceptionAsync_impone_el_empleado_y_el_tenant_de_la_peticion()
    {
        GivenEmployee();
        EmployeeException? creada = null;
        _repository.Setup(r => r.AddException(It.IsAny<EmployeeException>()))
            .Callback<EmployeeException>(e => creada = e);

        var result = await CreateService().AddExceptionAsync(
            EmployeeId,
            new CreateEmployeeExceptionRequest
            {
                StartDateTime = new DateTime(2026, 12, 24),
                EndDateTime = new DateTime(2026, 12, 26),
                Type = EmployeeExceptionTypes.Vacation,
                Reason = "  Navidad  ",
            });

        result.Success.Should().BeTrue();
        creada!.EmployeeId.Should().Be(EmployeeId);
        creada.OrganizationId.Should().Be(OrgA);
        creada.Reason.Should().Be("Navidad");
        creada.IsActive.Should().BeTrue();
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddExceptionAsync_sin_tenant_resuelto_no_toca_la_base()
    {
        var result = await CreateService(tenantResolved: false).AddExceptionAsync(
            EmployeeId, new CreateEmployeeExceptionRequest());

        result.ErrorCode.Should().Be(ErrorCodes.OrgTenantNotResolved);
        _repository.Verify(r => r.AddException(It.IsAny<EmployeeException>()), Times.Never);
    }

    [Fact]
    public async Task AddExceptionAsync_un_manager_no_puede_registrar_ausencias_de_un_admin()
    {
        GivenEmployee(rol: Roles.Admin);

        var result = await CreateService(callerRole: Roles.Manager).AddExceptionAsync(
            EmployeeId, new CreateEmployeeExceptionRequest());

        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
        _repository.Verify(r => r.AddException(It.IsAny<EmployeeException>()), Times.Never);
    }

    [Fact]
    public async Task DeleteExceptionAsync_hace_baja_logica_y_no_borra()
    {
        GivenEmployee();
        var exception = Exception();
        _repository
            .Setup(r => r.GetExceptionAsync(EmployeeId, 33, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exception);

        var result = await CreateService().DeleteExceptionAsync(EmployeeId, 33);

        result.Success.Should().BeTrue();
        exception.IsActive.Should().BeFalse();
        _repository.Verify(r => r.UpdateException(exception), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteExceptionAsync_sobre_una_ausencia_ya_retirada_no_vuelve_a_guardar()
    {
        GivenEmployee();
        _repository
            .Setup(r => r.GetExceptionAsync(EmployeeId, 33, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Exception(isActive: false));

        var result = await CreateService().DeleteExceptionAsync(EmployeeId, 33);

        result.Success.Should().BeTrue("la operación es idempotente");
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteExceptionAsync_de_una_ausencia_que_no_es_del_empleado_devuelve_no_encontrado()
    {
        GivenEmployee();
        _repository
            .Setup(r => r.GetExceptionAsync(EmployeeId, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeException?)null);

        var result = await CreateService().DeleteExceptionAsync(EmployeeId, 99);

        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    [Fact]
    public async Task DeleteExceptionAsync_un_manager_no_puede_retirar_ausencias_de_un_admin()
    {
        GivenEmployee(rol: Roles.Admin);

        var result = await CreateService(callerRole: Roles.Manager)
            .DeleteExceptionAsync(EmployeeId, 33);

        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
        _repository.Verify(
            r => r.GetExceptionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
