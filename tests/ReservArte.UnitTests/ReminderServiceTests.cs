using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Services;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Programación de recordatorios (RA-869d7f5zq) con dobles: qué se programa, qué
/// no y qué pasa si la cola falla. El recorrido completo, con base de datos, está
/// en <c>ReminderSchedulingTests</c> (integración).
/// </summary>
public class ReminderServiceTests
{
    private static readonly Guid Org = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    private const int AppointmentId = 40;

    /// <summary>Lunes 3 de marzo de 2031 a las 10:00 en Madrid = 09:00 UTC.</summary>
    private static readonly DateOnly Day = new(2031, 3, 3);

    private static readonly DateTimeOffset LongBefore = new(2031, 2, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IReminderRepository> _reminders = new();
    private readonly Mock<IAppointmentRepository> _appointments = new();
    private readonly Mock<IReminderJobScheduler> _jobs = new();
    private readonly Mock<IBusinessClock> _clock = new();
    private readonly Mock<ICurrentOrganizationService> _organization = new();
    private readonly List<ReminderLog> _added = [];
    private readonly List<(ReminderJobKey Key, DateTimeOffset SendAt)> _scheduled = [];
    private Appointment _appointment = Confirmed();

    public ReminderServiceTests()
    {
        _organization.SetupGet(o => o.OrganizationId).Returns(Org);
        _clock.Setup(c => c.FindTimeZoneAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"));
        _appointments.Setup(r => r.GetByIdAsync(AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _appointment);
        _reminders.Setup(r => r.GetLogsForAppointmentAsync(AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ReminderLog>());
        _reminders.Setup(r => r.AddLog(It.IsAny<ReminderLog>())).Callback<ReminderLog>(_added.Add);
        _jobs.Setup(j => j.Schedule(It.IsAny<ReminderJobKey>(), It.IsAny<DateTimeOffset>()))
            .Callback<ReminderJobKey, DateTimeOffset>((key, at) => _scheduled.Add((key, at)));
        Configurations(Reminder(1, 24));
    }

    private static Appointment Confirmed() => new()
    {
        Id = AppointmentId,
        OrganizationId = Org,
        AppointmentDate = Day,
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(11, 0),
        Status = AppointmentStatuses.Confirmed,
    };

    private static ReminderConfiguration Reminder(int id, int hours, string channel = ReminderChannels.Email) =>
        new() { Id = id, OrganizationId = Org, ReminderOrder = id, HoursBeforeAppointment = hours, Channel = channel };

    private void Configurations(params ReminderConfiguration[] configurations)
    {
        _reminders.Setup(r => r.GetActiveConfigurationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configurations);
        foreach (var configuration in configurations)
        {
            _reminders.Setup(r => r.GetConfigurationAsync(configuration.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(configuration);
        }
    }

    private ReminderService Service(DateTimeOffset now) =>
        new(
            _reminders.Object,
            _appointments.Object,
            _jobs.Object,
            _clock.Object,
            _organization.Object,
            new FixedTimeProvider(now),
            NullLogger<ReminderService>.Instance);

    // ── Programar ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Programa_un_aviso_pendiente_por_recordatorio_y_lo_pone_en_la_cola_a_su_hora()
    {
        await Service(LongBefore).ScheduleForAppointmentAsync(AppointmentId);

        _added.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            AppointmentId,
            ReminderConfigurationId = 1,
            Channel = ReminderChannels.Email,
            Status = ReminderLogStatuses.Pending,
            SentAt = (DateTime?)null,
        });
        _scheduled.Should().ContainSingle().Which.Should().Be((
            new ReminderJobKey(Org, AppointmentId, 1, ReminderChannels.Email),
            new DateTimeOffset(2031, 3, 2, 9, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task El_canal_both_genera_un_aviso_por_email_y_otro_por_WhatsApp()
    {
        Configurations(Reminder(1, 24, ReminderChannels.Both));

        await Service(LongBefore).ScheduleForAppointmentAsync(AppointmentId);

        _added.Select(l => l.Channel).Should().BeEquivalentTo(ReminderChannels.Email, ReminderChannels.WhatsApp);
        _scheduled.Select(s => s.Key.Channel).Should().BeEquivalentTo(ReminderChannels.Email, ReminderChannels.WhatsApp);
    }

    [Fact]
    public async Task Un_aviso_cuya_hora_ya_paso_no_se_programa_pero_los_demas_si()
    {
        Configurations(Reminder(1, 24), Reminder(2, 2));
        // Quedan 3 horas para la cita: el de 24 h ya no llega; el de 2 h, sí.
        var now = new DateTimeOffset(2031, 3, 3, 6, 0, 0, TimeSpan.Zero);

        await Service(now).ScheduleForAppointmentAsync(AppointmentId);

        _scheduled.Should().ContainSingle().Which.Key.ReminderConfigurationId.Should().Be(2);
        _added.Should().ContainSingle().Which.ReminderConfigurationId.Should().Be(2);
    }

    [Theory]
    [InlineData(AppointmentStatuses.Pending)]
    [InlineData(AppointmentStatuses.CancelledByCustomer)]
    [InlineData(AppointmentStatuses.Completed)]
    public async Task Solo_se_programan_los_avisos_de_una_cita_confirmada(string status)
    {
        _appointment.Status = status;

        await Service(LongBefore).ScheduleForAppointmentAsync(AppointmentId);

        _scheduled.Should().BeEmpty();
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task Al_reprogramar_no_duplica_el_aviso_pendiente_ni_repite_el_ya_enviado()
    {
        Configurations(Reminder(1, 24), Reminder(2, 2));
        _reminders.Setup(r => r.GetLogsForAppointmentAsync(AppointmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ReminderLog { Id = 1, ReminderConfigurationId = 1, Channel = ReminderChannels.Email, Status = ReminderLogStatuses.Sent },
                new ReminderLog { Id = 2, ReminderConfigurationId = 2, Channel = ReminderChannels.Email },
            });

        await Service(LongBefore).ScheduleForAppointmentAsync(AppointmentId);

        _added.Should().BeEmpty("los dos avisos ya tienen registro");
        _scheduled.Should().ContainSingle("el enviado no se repite")
            .Which.Key.ReminderConfigurationId.Should().Be(2);
    }

    [Fact]
    public async Task Si_la_cola_falla_la_confirmacion_no_se_entera()
    {
        _jobs.Setup(j => j.Schedule(It.IsAny<ReminderJobKey>(), It.IsAny<DateTimeOffset>()))
            .Throws(new InvalidOperationException("Hangfire sin conexión"));

        var act = () => Service(LongBefore).ScheduleForAppointmentAsync(AppointmentId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Los_registros_se_guardan_antes_de_poner_los_jobs_en_la_cola()
    {
        var order = new List<string>();
        _reminders.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("guardar")).ReturnsAsync(1);
        _jobs.Setup(j => j.Schedule(It.IsAny<ReminderJobKey>(), It.IsAny<DateTimeOffset>()))
            .Callback(() => order.Add("cola"));

        await Service(LongBefore).ScheduleForAppointmentAsync(AppointmentId);

        order.Should().Equal("guardar", "cola");
    }

    // ── Al llegar la hora ─────────────────────────────────────────────────

    private static readonly DateTimeOffset AtSendTime = new(2031, 3, 2, 9, 0, 5, TimeSpan.Zero);

    private void ExistingLog(ReminderLog? log) =>
        _reminders.Setup(r => r.GetLogAsync(AppointmentId, 1, ReminderChannels.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);

    [Fact]
    public async Task A_su_hora_el_aviso_pendiente_esta_listo_para_enviar()
    {
        ExistingLog(new ReminderLog { Id = 9 });

        var outcome = await Service(AtSendTime).ProcessDueAsync(AppointmentId, 1, ReminderChannels.Email);

        outcome.Should().Be(new ReminderDueOutcome(ReminderDueOutcomes.Ready, 9));
    }

    [Fact]
    public async Task Si_la_cita_se_movio_a_mas_tarde_el_job_antiguo_no_hace_nada()
    {
        ExistingLog(new ReminderLog { Id = 9 });
        _appointment.AppointmentDate = Day.AddDays(1);

        var outcome = await Service(AtSendTime).ProcessDueAsync(AppointmentId, 1, ReminderChannels.Email);

        outcome.Value.Should().Be(ReminderDueOutcomes.NotYet);
        _reminders.Verify(r => r.RemoveLog(It.IsAny<ReminderLog>()), Times.Never);
    }

    [Theory]
    [InlineData(ReminderLogStatuses.Sent)]
    [InlineData(ReminderLogStatuses.Failed)]
    [InlineData(ReminderLogStatuses.Delivered)]
    public async Task Un_aviso_ya_tratado_no_se_vuelve_a_tratar(string status)
    {
        ExistingLog(new ReminderLog { Id = 9, Status = status });

        var outcome = await Service(AtSendTime).ProcessDueAsync(AppointmentId, 1, ReminderChannels.Email);

        outcome.Should().Be(new ReminderDueOutcome(ReminderDueOutcomes.AlreadyHandled, 9));
    }

    [Theory]
    [InlineData(AppointmentStatuses.CancelledByBusiness)]
    [InlineData(AppointmentStatuses.CancelledByCustomer)]
    [InlineData(AppointmentStatuses.Pending)]
    public async Task Si_la_cita_ya_no_esta_confirmada_el_aviso_pendiente_se_descarta(string status)
    {
        var log = new ReminderLog { Id = 9 };
        ExistingLog(log);
        _appointment.Status = status;

        var outcome = await Service(AtSendTime).ProcessDueAsync(AppointmentId, 1, ReminderChannels.Email);

        outcome.Value.Should().Be(ReminderDueOutcomes.Discarded);
        _reminders.Verify(r => r.RemoveLog(log), Times.Once);
    }

    [Fact]
    public async Task Si_el_recordatorio_se_desactivo_el_aviso_se_descarta()
    {
        ExistingLog(new ReminderLog { Id = 9 });
        var inactive = Reminder(1, 24);
        inactive.IsActive = false;
        _reminders.Setup(r => r.GetConfigurationAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(inactive);

        var outcome = await Service(AtSendTime).ProcessDueAsync(AppointmentId, 1, ReminderChannels.Email);

        outcome.Value.Should().Be(ReminderDueOutcomes.Discarded);
    }

    [Fact]
    public async Task Un_job_sin_registro_lo_crea_al_llegar_su_hora()
    {
        ExistingLog(null);

        var outcome = await Service(AtSendTime).ProcessDueAsync(AppointmentId, 1, ReminderChannels.Email);

        outcome.Value.Should().Be(ReminderDueOutcomes.Ready);
        _added.Should().ContainSingle().Which.Status.Should().Be(ReminderLogStatuses.Pending);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
