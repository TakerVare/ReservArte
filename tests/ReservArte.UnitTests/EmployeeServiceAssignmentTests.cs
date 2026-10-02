using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Validators.Employees;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Options;
using ReservArte.Infrastructure.Persistence;
using ReservArte.Infrastructure.Persistence.Repositories;
using ReservArte.Infrastructure.Services;
using ReservArte.Shared.Api;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Servicios que presta cada empleado (4.1b): el reemplazo del conjunto en el
/// repositorio (SQLite en memoria), las reglas del servicio (dobles) y el validador.
/// El mapeo está en MappingCharacterizationTests.
/// </summary>
public class EmployeeServiceAssignmentTests : IDisposable
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    private const int Ana = 1;
    private const int Diana = 4;

    // Servicios: 10 y 11 activos en A; 12 retirado en A; 20 activo en B.
    private const int Cejas = 10;
    private const int Pestanas = 11;
    private const int Retirado = 12;
    private const int DeOtroCentro = 20;

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public EmployeeServiceAssignmentTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var context = CreateContext(organizationId: null);
        context.Database.EnsureCreated();
        Seed(context);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class FakeCurrentOrganization : ICurrentOrganizationService
    {
        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;

        public static FakeCurrentOrganization For(Guid? organizationId)
        {
            var fake = new FakeCurrentOrganization();
            if (organizationId is { } id)
            {
                fake.SetOrganization(id);
            }

            return fake;
        }
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public int? UserId { get; init; }

        public string? Role { get; init; }
    }

    private AppDbContext CreateContext(Guid? organizationId) =>
        new(_options, FakeCurrentOrganization.For(organizationId));

    private EmployeeRepository CreateRepository(AppDbContext context, Guid? organizationId) =>
        new(context, FakeCurrentOrganization.For(organizationId));

    private static void Seed(AppDbContext context)
    {
        context.Organizations.AddRange(
            new Organization { Id = OrgA, Name = "More Than Brows", Subdomain = "morethanbrows" },
            new Organization { Id = OrgB, Name = "Otro Centro", Subdomain = "otrocentro" });

        context.Users.AddRange(NewUser(Ana, OrgA, "ana@reservarte.com"), NewUser(Diana, OrgB, "diana@otro.com"));
        context.Employees.AddRange(
            NewEmployee(Ana, OrgA, "ana@reservarte.com"),
            NewEmployee(Diana, OrgB, "diana@otro.com"));

        context.Services.AddRange(
            NewService(Cejas, OrgA, "Henna de cejas"),
            NewService(Pestanas, OrgA, "Lifting de pestañas"),
            NewService(Retirado, OrgA, "Antiguo", isActive: false),
            NewService(DeOtroCentro, OrgB, "Servicio de B"));

        context.EmployeeServices.AddRange(
            new EmployeeServiceAssignment { EmployeeId = Ana, ServiceId = Pestanas, OrganizationId = OrgA, ProficiencyLevel = 4 },
            new EmployeeServiceAssignment { EmployeeId = Ana, ServiceId = Cejas, OrganizationId = OrgA, ProficiencyLevel = 2, IsActive = false },
            new EmployeeServiceAssignment { EmployeeId = Diana, ServiceId = DeOtroCentro, OrganizationId = OrgB });

        context.SaveChanges();
    }

    private static User NewUser(int id, Guid organizationId, string email) => new()
    {
        Id = id,
        OrganizationId = organizationId,
        FirstName = "N",
        LastName = "A",
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Rol = Roles.Employee,
        SecurityStamp = Guid.NewGuid().ToString(),
    };

    private static Employee NewEmployee(int id, Guid organizationId, string email) => new()
    {
        Id = id,
        OrganizationId = organizationId,
        FirstName = "N",
        LastName = "A",
        Email = email,
        Rol = Roles.Employee,
    };

    private static Service NewService(int id, Guid organizationId, string name, bool isActive = true) => new()
    {
        Id = id,
        OrganizationId = organizationId,
        Name = name,
        DurationMinutes = 40,
        BasePrice = 20,
        IsActive = isActive,
    };

    // ── Repositorio ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetServiceAssignmentsAsync_da_solo_las_activas_con_su_servicio()
    {
        using var context = CreateContext(OrgA);

        var assignments = await CreateRepository(context, OrgA).GetServiceAssignmentsAsync(Ana);

        assignments.Select(a => (a.ServiceId, a.Service.Name, a.ProficiencyLevel))
            .Should().Equal((Pestanas, "Lifting de pestañas", 4));
    }

    [Fact]
    public async Task GetAssignableServiceIdsAsync_descarta_los_retirados_y_los_de_otro_centro()
    {
        using var context = CreateContext(OrgA);

        var ids = await CreateRepository(context, OrgA)
            .GetAssignableServiceIdsAsync(new[] { Cejas, Retirado, DeOtroCentro, 999 });

        ids.Should().BeEquivalentTo(new[] { Cejas });
    }

    [Fact]
    public async Task ReplaceServiceAssignmentsAsync_reactiva_con_su_nivel_da_de_baja_lo_que_sale_y_crea_con_nivel_1()
    {
        using (var context = CreateContext(OrgA))
        {
            var repository = CreateRepository(context, OrgA);
            await repository.ReplaceServiceAssignmentsAsync(Ana, new[] { Cejas });
            await repository.SaveChangesAsync();
        }

        using (var context = CreateContext(OrgA))
        {
            var rows = await context.EmployeeServices.Where(a => a.EmployeeId == Ana)
                .OrderBy(a => a.ServiceId).ToListAsync();
            rows.Select(a => (a.ServiceId, a.IsActive, a.ProficiencyLevel)).Should().Equal(
                (Cejas, true, 2),       // vuelve, con el nivel que tenía
                (Pestanas, false, 4));  // sale: baja lógica, no se borra
        }

        using (var context = CreateContext(OrgA))
        {
            var repository = CreateRepository(context, OrgA);
            await repository.ReplaceServiceAssignmentsAsync(Ana, new[] { Cejas, Pestanas, Retirado });
            await repository.SaveChangesAsync();
        }

        using (var context = AppDbContext.ForSystem(_options, "tests: comprobar todos los centros"))
        {
            var nueva = await context.EmployeeServices.SingleAsync(a => a.EmployeeId == Ana && a.ServiceId == Retirado);
            nueva.ProficiencyLevel.Should().Be(1);
            nueva.OrganizationId.Should().Be(OrgA);
            nueva.IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ReplaceServiceAssignmentsAsync_no_toca_las_asignaciones_de_otro_centro()
    {
        using (var context = CreateContext(OrgB))
        {
            var repository = CreateRepository(context, OrgB);
            await repository.ReplaceServiceAssignmentsAsync(Ana, Array.Empty<int>());
            await repository.SaveChangesAsync();
        }

        using var check = AppDbContext.ForSystem(_options, "tests: comprobar todos los centros");
        (await check.EmployeeServices.SingleAsync(a => a.EmployeeId == Ana && a.ServiceId == Pestanas))
            .IsActive.Should().BeTrue();
    }

    // ── Servicio ──────────────────────────────────────────────────────────

    private readonly Mock<IEmployeeRepository> _repository = new();

    private EmployeeService CreateService(string callerRole = Roles.Admin)
    {
        var tenant = new FakeCurrentOrganization();
        tenant.SetOrganization(OrgA);
        var store = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _repository
            .Setup(r => r.GetServiceAssignmentsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmployeeServiceAssignment>());

        return new EmployeeService(
            _repository.Object,
            new FakeUnitOfWork(),
            userManager.Object,
            tenant,
            new FakeCurrentUser { UserId = 99, Role = callerRole },
            Mock.Of<IEmailService>(),
            Options.Create(new AppOptions { FrontendBaseUrl = "http://localhost:3000" }),
            NullLogger<EmployeeService>.Instance);
    }

    private void GivenEmployee(string rol = Roles.Employee) =>
        _repository
            .Setup(r => r.GetByIdAsync(Ana, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = Ana, OrganizationId = OrgA, Rol = rol, Email = "ana@reservarte.com" });

    private void GivenAssignable(params int[] ids) =>
        _repository
            .Setup(r => r.GetAssignableServiceIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids);

    [Fact]
    public async Task Los_servicios_de_un_empleado_inexistente_dan_404()
    {
        var result = await CreateService().GetServicesAsync(Ana);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    [Fact]
    public async Task Reemplazar_pasa_al_repositorio_los_ids_sin_repetir()
    {
        GivenEmployee();
        GivenAssignable(Cejas, Pestanas);

        var result = await CreateService().ReplaceServicesAsync(
            Ana, new UpdateEmployeeServicesRequest { ServiceIds = new[] { Cejas, Pestanas, Cejas } });

        result.Success.Should().BeTrue();
        _repository.Verify(r => r.ReplaceServiceAssignmentsAsync(
            Ana,
            It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { Cejas, Pestanas })),
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Un_servicio_inexistente_o_retirado_da_400_con_su_indice_y_no_guarda_nada()
    {
        GivenEmployee();
        GivenAssignable(Cejas);

        var result = await CreateService().ReplaceServicesAsync(
            Ana, new UpdateEmployeeServicesRequest { ServiceIds = new[] { Cejas, Retirado } });

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        result.ErrorDetails.Should().BeAssignableTo<IEnumerable<ApiErrorDetail>>()
            .Which.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Field = "serviceIds[1]", Code = "UnknownService" });
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Un_Manager_no_cambia_los_servicios_de_un_Admin()
    {
        GivenEmployee(Roles.Admin);
        GivenAssignable(Cejas);

        var result = await CreateService(Roles.Manager).ReplaceServicesAsync(
            Ana, new UpdateEmployeeServicesRequest { ServiceIds = new[] { Cejas } });

        result.ErrorCode.Should().Be(ErrorCodes.GenForbidden);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Validador ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(new int[0], true)]
    [InlineData(new[] { 1, 2 }, true)]
    [InlineData(new[] { 0 }, false)]
    [InlineData(new[] { 3, -1 }, false)]
    public void El_validador_admite_una_lista_vacia_y_rechaza_ids_no_positivos(int[] ids, bool valid)
    {
        new UpdateEmployeeServicesRequestValidator()
            .Validate(new UpdateEmployeeServicesRequest { ServiceIds = ids })
            .IsValid.Should().Be(valid);
    }

    [Fact]
    public void El_validador_pone_tope_al_numero_de_servicios()
    {
        var ids = Enumerable.Range(1, UpdateEmployeeServicesRequestValidator.MaxServices + 1).ToArray();

        new UpdateEmployeeServicesRequestValidator()
            .Validate(new UpdateEmployeeServicesRequest { ServiceIds = ids })
            .IsValid.Should().BeFalse();
    }
}
