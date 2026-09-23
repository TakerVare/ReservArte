using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Services;
using ReservArte.Shared.Api;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Cálculo de huecos y detección de solapes (RA-869d7f4rd). El acceso a datos
/// va doble: el contrato real de los repositorios lo cubren
/// EmployeeRepositoryTests y AppointmentRepositoryTests contra SQLite.
///
/// Las fechas de las pruebas son futuras respecto al reloj falso a propósito,
/// para que el descarte de huecos pasados no se cuele en los casos que miden
/// otra cosa; el descarte tiene sus propias pruebas.
/// </summary>
public class AvailabilityServiceTests
{
    private static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

    private const int EmployeeId = 7;

    /// <summary>Miércoles, lo bastante lejos del reloj falso para no ser hoy.</summary>
    private static readonly DateOnly Date = new(2026, 10, 14);

    /// <summary>2026-09-23 09:20 UTC = 11:20 en Madrid (horario de verano).</summary>
    private static readonly DateTimeOffset Now =
        new(2026, 9, 23, 9, 20, 0, TimeSpan.Zero);

    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly FakeCurrentOrganization _currentOrganization = new();

    /// <summary>
    /// Por defecto: empleado activo, sin horario, sin ausencias y sin citas.
    /// Cada prueba añade solo lo suyo. Sin estos valores, Moq devolvería null
    /// dentro de los `Task&lt;IReadOnlyList&lt;T&gt;&gt;` y reventaría el cálculo.
    /// </summary>
    public AvailabilityServiceTests()
    {
        _currentOrganization.SetOrganization(OrgA);

        _employees
            .Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = EmployeeId, OrganizationId = OrgA, IsActive = true });

        _employees
            .Setup(r => r.GetAvailabilitiesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmployeeAvailability>());

        _employees
            .Setup(r => r.GetExceptionsAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmployeeException>());

        _appointments
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Appointment>());
    }

    // ── Huecos libres ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableSlots_sin_organizacion_resuelta_falla()
    {
        _currentOrganization.Clear();

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.OrgTenantNotResolved);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    [InlineData(AvailabilityService.MaxDurationMinutes + 1)]
    public async Task GetAvailableSlots_con_duracion_fuera_de_rango_falla(int durationMinutes)
    {
        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, durationMinutes);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
        result.ErrorDetails.Should().BeAssignableTo<IEnumerable<ApiErrorDetail>>()
            .Which.Should().ContainSingle(d => d.Field == "durationMinutes");
    }

    [Fact]
    public async Task GetAvailableSlots_de_un_empleado_inexistente_es_404()
    {
        _employees
            .Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    /// <summary>
    /// Un empleado de baja no tiene agenda que ofrecer, y se responde igual que
    /// si no existiera: distinguirlos no aporta a quien consulta huecos.
    /// </summary>
    [Fact]
    public async Task GetAvailableSlots_de_un_empleado_de_baja_es_404()
    {
        _employees
            .Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = EmployeeId, OrganizationId = OrgA, IsActive = false });

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    /// <summary>Sin horario ese día la respuesta es 200 con la lista vacía, no un error.</summary>
    [Fact]
    public async Task GetAvailableSlots_sin_horario_ese_dia_devuelve_lista_vacia()
    {
        GivenSchedule(OtherDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        result.Success.Should().BeTrue();
        result.Data!.Slots.Should().BeEmpty();
    }

    /// <summary>
    /// 09:00-14:00 son 300 minutos; con citas de 60 y rejilla de 15, caben 17
    /// inicios (09:00, 09:15 … 13:00). El último hueco no puede rebasar el
    /// final del tramo.
    /// </summary>
    [Fact]
    public async Task GetAvailableSlots_recorre_el_tramo_con_rejilla_de_quince_minutos()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        result.Success.Should().BeTrue();
        var slots = result.Data!.Slots;

        slots.Should().HaveCount(17);
        slots[0].StartTime.Should().Be(new TimeOnly(9, 0));
        slots[0].EndTime.Should().Be(new TimeOnly(10, 0));
        slots[1].StartTime.Should().Be(new TimeOnly(9, 15));
        slots[^1].StartTime.Should().Be(new TimeOnly(13, 0));
        slots[^1].EndTime.Should().Be(new TimeOnly(14, 0));
        result.Data.SlotStepMinutes.Should().Be(AvailabilityService.SlotStepMinutes);
    }

    /// <summary>
    /// Una cita viva de 10:00 a 11:00 no deja empezar nada entre las 09:15 y las
    /// 11:00: un hueco de 60 minutos que arrancara ahí la pisaría.
    /// </summary>
    [Fact]
    public async Task GetAvailableSlots_descuenta_las_citas_que_ocupan_agenda()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));
        GivenAppointments(Appointment(new TimeOnly(10, 0), new TimeOnly(11, 0), AppointmentStatuses.Confirmed));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        var starts = result.Data!.Slots.Select(s => s.StartTime).ToList();

        starts.Should().Contain(new TimeOnly(9, 0));
        starts.Should().NotContain(new TimeOnly(9, 15));
        starts.Should().NotContain(new TimeOnly(10, 30));
        starts.Should().Contain(new TimeOnly(11, 0));
    }

    /// <summary>
    /// Cancelada o no presentada liberan el hueco: solo bloquean los estados de
    /// <see cref="AppointmentStatuses.Blocking"/>.
    /// </summary>
    [Theory]
    [InlineData(AppointmentStatuses.Cancelled)]
    [InlineData(AppointmentStatuses.CancelledByCustomer)]
    [InlineData(AppointmentStatuses.CancelledByBusiness)]
    [InlineData(AppointmentStatuses.NoShow)]
    [InlineData(AppointmentStatuses.Completed)]
    public async Task GetAvailableSlots_no_descuenta_las_citas_que_no_ocupan_agenda(string status)
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));
        GivenAppointments(Appointment(new TimeOnly(10, 0), new TimeOnly(11, 0), status));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        result.Data!.Slots.Select(s => s.StartTime).Should().Contain(new TimeOnly(10, 0));
    }

    [Theory]
    [InlineData(AppointmentStatuses.Pending)]
    [InlineData(AppointmentStatuses.Confirmed)]
    [InlineData(AppointmentStatuses.InProgress)]
    public async Task GetAvailableSlots_descuenta_los_tres_estados_que_ocupan_agenda(string status)
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));
        GivenAppointments(Appointment(new TimeOnly(10, 0), new TimeOnly(11, 0), status));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        result.Data!.Slots.Select(s => s.StartTime).Should().NotContain(new TimeOnly(10, 0));
    }

    [Fact]
    public async Task GetAvailableSlots_descuenta_las_ausencias()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));
        GivenExceptions(Exception(Date.ToDateTime(new TimeOnly(9, 0)), Date.ToDateTime(new TimeOnly(12, 0))));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        var starts = result.Data!.Slots.Select(s => s.StartTime).ToList();

        starts.Should().NotContain(new TimeOnly(9, 0));
        starts.Should().NotContain(new TimeOnly(11, 30));
        starts.Should().Contain(new TimeOnly(12, 0));
    }

    /// <summary>
    /// Una ausencia de varios días cubre este por completo aunque empiece y
    /// acabe fuera: el recorte al día no debe dejar pasar huecos.
    /// </summary>
    [Fact]
    public async Task GetAvailableSlots_aplica_una_ausencia_que_envuelve_el_dia()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));
        GivenExceptions(Exception(
            Date.AddDays(-2).ToDateTime(new TimeOnly(8, 0)),
            Date.AddDays(3).ToDateTime(new TimeOnly(20, 0))));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        result.Data!.Slots.Should().BeEmpty();
    }

    /// <summary>
    /// El día del horario es el del proyecto (lunes = 0), no el `int` de
    /// <see cref="System.DayOfWeek"/> (domingo = 0). Los dos nunca coinciden,
    /// así que un tramo con cada convención distingue cuál se está usando.
    /// </summary>
    [Fact]
    public async Task GetAvailableSlots_usa_la_convencion_de_dia_del_proyecto()
    {
        GivenSchedule(
            (ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(11, 0)),
            ((int)Date.DayOfWeek, new TimeOnly(16, 0), new TimeOnly(20, 0)));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        var starts = result.Data!.Slots.Select(s => s.StartTime).ToList();

        starts.Should().Contain(new TimeOnly(9, 0));
        starts.Should().NotContain(new TimeOnly(16, 0));
    }

    [Fact]
    public async Task GetAvailableSlots_de_un_dia_pasado_devuelve_lista_vacia()
    {
        GivenSchedule(ProjectDayOf(Yesterday), new TimeOnly(9, 0), new TimeOnly(14, 0));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Yesterday, 60);

        result.Success.Should().BeTrue();
        result.Data!.Slots.Should().BeEmpty();
    }

    /// <summary>
    /// Hoy a las 11:20 de Madrid (09:20 UTC): el primer hueco es el siguiente
    /// punto de la rejilla anclada a las 09:00, o sea las 11:30. Si el servicio
    /// razonara en UTC daría las 09:30, y si no filtrara, las 09:00.
    /// </summary>
    [Fact]
    public async Task GetAvailableSlots_de_hoy_descarta_los_huecos_ya_pasados()
    {
        var today = TodayInMadrid;
        GivenSchedule(ProjectDayOf(today), new TimeOnly(9, 0), new TimeOnly(18, 0));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, today, 60);

        result.Data!.Slots.Should().NotBeEmpty();
        result.Data.Slots[0].StartTime.Should().Be(new TimeOnly(11, 30));
    }

    /// <summary>
    /// La respuesta repite la petición para que el frontend pueda lanzar varias
    /// consultas a la vez y saber a cuál corresponde cada lista.
    /// </summary>
    [Fact]
    public async Task GetAvailableSlots_devuelve_la_peticion_en_la_respuesta()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));

        var result = await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 45);

        result.Data!.EmployeeId.Should().Be(EmployeeId);
        result.Data.Date.Should().Be(Date);
        result.Data.DurationMinutes.Should().Be(45);
        result.Data.Slots.Should().OnlyContain(
            s => s.EndTime == s.StartTime.AddMinutes(45));
    }

    // ── Validación de solapes ─────────────────────────────────────────────

    [Fact]
    public async Task EnsureSlotAvailable_en_hueco_libre_dentro_del_horario_pasa()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(10, 0), new TimeOnly(11, 0));

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureSlotAvailable_fuera_del_horario_falla()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(15, 0), new TimeOnly(16, 0));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.AptSlotUnavailable);
    }

    /// <summary>Que empiece dentro no basta: tiene que caber entero.</summary>
    [Fact]
    public async Task EnsureSlotAvailable_que_se_sale_del_horario_por_el_final_falla()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(13, 30), new TimeOnly(14, 30));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.AptSlotUnavailable);
    }

    [Fact]
    public async Task EnsureSlotAvailable_sobre_una_cita_viva_falla()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));
        GivenAppointments(Appointment(new TimeOnly(10, 0), new TimeOnly(11, 0), AppointmentStatuses.Pending));

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(10, 30), new TimeOnly(11, 30));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.AptSlotUnavailable);
    }

    /// <summary>
    /// El intervalo es semiabierto: empezar justo cuando acaba la anterior no es
    /// solapar. Si esto fallara, la agenda dejaría un hueco muerto entre citas.
    /// </summary>
    [Fact]
    public async Task EnsureSlotAvailable_pegado_al_final_de_otra_cita_pasa()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));
        GivenAppointments(Appointment(new TimeOnly(10, 0), new TimeOnly(11, 0), AppointmentStatuses.Confirmed));

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(11, 0), new TimeOnly(12, 0));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureSlotAvailable_sobre_una_cita_cancelada_pasa()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));
        GivenAppointments(Appointment(
            new TimeOnly(10, 0), new TimeOnly(11, 0), AppointmentStatuses.CancelledByCustomer));

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(10, 0), new TimeOnly(11, 0));

        result.Success.Should().BeTrue();
    }

    /// <summary>Reagendar una cita no puede chocar consigo misma.</summary>
    [Fact]
    public async Task EnsureSlotAvailable_ignora_la_cita_excluida()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));
        var existing = Appointment(new TimeOnly(10, 0), new TimeOnly(11, 0), AppointmentStatuses.Confirmed);
        existing.Id = 42;
        GivenAppointments(existing);

        var sinExcluir = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(10, 30), new TimeOnly(11, 30));

        var excluyendo = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(10, 30), new TimeOnly(11, 30), excludeAppointmentId: 42);

        sinExcluir.Success.Should().BeFalse();
        excluyendo.Success.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureSlotAvailable_sobre_una_ausencia_falla()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));
        GivenExceptions(Exception(
            Date.ToDateTime(new TimeOnly(10, 0)), Date.ToDateTime(new TimeOnly(12, 0))));

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(11, 0), new TimeOnly(12, 0));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.AptSlotUnavailable);
    }

    [Fact]
    public async Task EnsureSlotAvailable_con_fin_anterior_al_inicio_falla()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(12, 0), new TimeOnly(11, 0));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenValidationFailed);
    }

    [Fact]
    public async Task EnsureSlotAvailable_de_un_empleado_de_baja_es_404()
    {
        _employees
            .Setup(r => r.GetByIdAsync(EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee { Id = EmployeeId, OrganizationId = OrgA, IsActive = false });

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(10, 0), new TimeOnly(11, 0));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.GenNotFound);
    }

    [Fact]
    public async Task EnsureSlotAvailable_sin_organizacion_resuelta_falla()
    {
        _currentOrganization.Clear();

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Date, new TimeOnly(10, 0), new TimeOnly(11, 0));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCodes.OrgTenantNotResolved);
    }

    /// <summary>
    /// A diferencia de la consulta de huecos, validar un tramo NO mira el reloj:
    /// el personal registra a veces una cita que acaba de ocurrir.
    /// </summary>
    [Fact]
    public async Task EnsureSlotAvailable_en_una_fecha_pasada_no_mira_el_reloj()
    {
        GivenSchedule(ProjectDayOf(Yesterday), new TimeOnly(9, 0), new TimeOnly(14, 0));

        var result = await CreateService().EnsureSlotAvailableAsync(
            EmployeeId, Yesterday, new TimeOnly(10, 0), new TimeOnly(11, 0));

        result.Success.Should().BeTrue();
    }

    /// <summary>
    /// La agenda se pide siempre acotada a ese empleado y a ese único día: si
    /// se pidiera el rango entero se traerían citas de otras personas y otras
    /// fechas para descartarlas en memoria.
    /// </summary>
    [Fact]
    public async Task GetAvailableSlots_consulta_solo_la_agenda_de_ese_empleado_y_dia()
    {
        GivenSchedule(ProjectDayOf(Date), new TimeOnly(9, 0), new TimeOnly(14, 0));

        await CreateService().GetAvailableSlotsAsync(EmployeeId, Date, 60);

        _appointments.Verify(
            r => r.GetByDateRangeAsync(Date, Date, EmployeeId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Andamiaje ─────────────────────────────────────────────────────────

    private static DateOnly TodayInMadrid =>
        DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(
                Now, TimeZoneInfo.FindSystemTimeZoneById(AvailabilityService.BusinessTimeZoneId))
                .DateTime);

    private static DateOnly Yesterday => TodayInMadrid.AddDays(-1);

    private static int ProjectDayOf(DateOnly date) => WeekDay.FromDate(date);

    /// <summary>Un día del proyecto que no es el de esa fecha.</summary>
    private static int OtherDayOf(DateOnly date) => (WeekDay.FromDate(date) + 1) % 7;

    private void GivenSchedule(int projectDay, TimeOnly start, TimeOnly end) =>
        GivenSchedule((projectDay, start, end));

    private void GivenSchedule(params (int Day, TimeOnly Start, TimeOnly End)[] windows) =>
        _employees
            .Setup(r => r.GetAvailabilitiesAsync(EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(windows
                .Select(w => new EmployeeAvailability
                {
                    OrganizationId = OrgA,
                    EmployeeId = EmployeeId,
                    DayOfWeek = w.Day,
                    StartTime = w.Start,
                    EndTime = w.End,
                })
                .ToList());

    private void GivenAppointments(params Appointment[] appointments) =>
        _appointments
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(appointments);

    private void GivenExceptions(params EmployeeException[] exceptions) =>
        _employees
            .Setup(r => r.GetExceptionsAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exceptions);

    private static Appointment Appointment(TimeOnly start, TimeOnly end, string status) =>
        new()
        {
            OrganizationId = OrgA,
            EmployeeId = EmployeeId,
            AppointmentDate = Date,
            StartTime = start,
            EndTime = end,
            Status = status,
        };

    private static EmployeeException Exception(DateTime start, DateTime end) =>
        new()
        {
            OrganizationId = OrgA,
            EmployeeId = EmployeeId,
            StartDateTime = start,
            EndDateTime = end,
            Type = EmployeeExceptionTypes.Vacation,
        };

    private AvailabilityService CreateService() =>
        new(
            _employees.Object,
            _appointments.Object,
            _currentOrganization,
            new FixedTimeProvider(Now),
            NullLogger<AvailabilityService>.Instance);

    /// <summary>
    /// Reloj congelado. Se deriva de <see cref="TimeProvider"/> en lugar de
    /// traer `Microsoft.Extensions.TimeProvider.Testing`: solo hace falta fijar
    /// el instante, y no merece un paquete más.
    /// </summary>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeCurrentOrganization : ICurrentOrganizationService
    {
        public Guid? OrganizationId { get; private set; }

        public bool IsResolved => OrganizationId.HasValue;

        public void SetOrganization(Guid organizationId) => OrganizationId = organizationId;

        public void Clear() => OrganizationId = null;
    }
}
