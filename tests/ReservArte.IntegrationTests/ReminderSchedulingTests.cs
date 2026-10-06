using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Jobs;
using ReservArte.Infrastructure.Persistence;
using ReservArte.IntegrationTests.Infrastructure;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Programación de recordatorios (RA-869d7f5zq) por la API y contra PostgreSQL:
/// confirmar una cita registra sus avisos y los pone en la cola a su hora; moverla
/// los reprograma sin duplicarlos; y el job, al dispararse, fija el centro y
/// vuelve a mirar la cita. La cola es <see cref="ApiFactory.ReminderJobs"/>: Hangfire
/// no corre en los tests. Cada test usa un centro propio.
/// </summary>
[Collection(ApiCollection.Name)]
public class ReminderSchedulingTests(ApiFactory factory)
{
    private const string Appointments = "/api/v1/appointments";

    /// <summary>Lunes 3 de marzo de 2031: invierno, UTC+1 en la península.</summary>
    private static readonly DateOnly Day = AppointmentScene.Day;

    private static DateTimeOffset Utc(int day, int hour) => new(2031, 3, day, hour, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Confirmar_una_cita_registra_sus_avisos_y_los_programa_a_su_hora()
    {
        var center = await CenterAsync();
        var id = await BookAsync(center, new TimeOnly(10, 0));
        factory.ReminderJobs.For(id).Should().BeEmpty("una cita pendiente no tiene recordatorios");

        var confirmed = await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/confirm", center.Org, center.Token);

        confirmed.Status.Should().Be(HttpStatusCode.OK);
        confirmed.ShouldBeEnvelope(success: true);

        // 24 h antes por email (domingo 10:00 de Madrid) y 2 h antes por los dos
        // canales (lunes 08:00, fuera de su franja: sale a las 09:00).
        factory.ReminderJobs.For(id).Should().BeEquivalentTo(new[]
        {
            (new ReminderJobKey(center.Org, id, center.DayBefore.Id, ReminderChannels.Email), Utc(2, 9)),
            (new ReminderJobKey(center.Org, id, center.SameDay.Id, ReminderChannels.Email), Utc(3, 8)),
            (new ReminderJobKey(center.Org, id, center.SameDay.Id, ReminderChannels.WhatsApp), Utc(3, 8)),
        });

        (await LogsAsync(center.Org, id)).Should().BeEquivalentTo(new[]
        {
            new { ReminderConfigurationId = center.DayBefore.Id, Channel = "email", Status = "pending", SentAt = (DateTime?)null },
            new { ReminderConfigurationId = center.SameDay.Id, Channel = "email", Status = "pending", SentAt = (DateTime?)null },
            new { ReminderConfigurationId = center.SameDay.Id, Channel = "whatsapp", Status = "pending", SentAt = (DateTime?)null },
        });
    }

    [Fact]
    public async Task Mover_una_cita_confirmada_reprograma_sus_avisos_sin_duplicar_los_registros()
    {
        var center = await CenterAsync();
        var id = await BookAsync(center, new TimeOnly(10, 0));
        await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/confirm", center.Org, center.Token);

        var moved = await factory.SendAsync(
            HttpMethod.Put, $"{Appointments}/{id}", center.Org, center.Token, center.Scene.Tinting(new TimeOnly(16, 0)));

        moved.Status.Should().Be(HttpStatusCode.OK);
        factory.ReminderJobs.For(id).Skip(3).Should().BeEquivalentTo(new[]
        {
            (new ReminderJobKey(center.Org, id, center.DayBefore.Id, ReminderChannels.Email), Utc(2, 15)),
            (new ReminderJobKey(center.Org, id, center.SameDay.Id, ReminderChannels.Email), Utc(3, 13)),
            (new ReminderJobKey(center.Org, id, center.SameDay.Id, ReminderChannels.WhatsApp), Utc(3, 13)),
        });
        (await LogsAsync(center.Org, id)).Should().HaveCount(3);
    }

    [Fact]
    public async Task Con_el_centro_en_Canarias_los_avisos_salen_una_hora_mas_tarde_en_UTC()
    {
        var center = await CenterAsync();
        // La configuración la cambia la dirección, no una empleada.
        var admin = await factory.CreateEmployeeAsync(center.Org, Roles.Admin);
        (await factory.SendAsync(
            HttpMethod.Put, "/api/v1/organization/settings", center.Org,
            await factory.TokenForAsync(center.Org, admin.Id),
            new { timeZone = "Atlantic/Canary", cancellationHoursThreshold = 24, maxNoShowsBeforeBlock = 3 }))
            .Status.Should().Be(HttpStatusCode.OK);
        var id = await BookAsync(center, new TimeOnly(10, 0));

        await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/confirm", center.Org, center.Token);

        factory.ReminderJobs.For(id).Select(j => j.SendAt).Should().BeEquivalentTo([Utc(2, 10), Utc(3, 9), Utc(3, 9)]);
    }

    [Fact]
    public async Task Un_centro_sin_recordatorios_confirma_citas_sin_programar_nada()
    {
        var org = await factory.CreateOrganizationAsync();
        var scene = await factory.CreateAppointmentSceneAsync(org);
        var token = await factory.TokenForAsync(org, scene.Employee.Id);
        var id = await BookAsync(new Center(org, token, scene, null!, null!), new TimeOnly(10, 0));

        var confirmed = await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/confirm", org, token);

        confirmed.Status.Should().Be(HttpStatusCode.OK);
        factory.ReminderJobs.For(id).Should().BeEmpty();
        (await LogsAsync(org, id)).Should().BeEmpty();
    }

    [Fact]
    public async Task El_centro_piloto_nace_con_su_recordatorio_por_defecto()
    {
        await using var scope = factory.CreateTenantScope(TestData.OrgA);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reminder = await db.ReminderConfigurations.Include(r => r.MessageTemplate).SingleAsync(r => r.ReminderOrder == 1);

        reminder.Should().BeEquivalentTo(new
        {
            HoursBeforeAppointment = 24,
            Channel = ReminderChannels.Email,
            IsActive = true,
            AllowedSendStartTime = new TimeOnly(9, 0),
            AllowedSendEndTime = new TimeOnly(21, 0),
        });
        reminder.MessageTemplate.Type.Should().Be(MessageTemplateTypes.EmailReminder);
        reminder.MessageTemplate.Body.Should().Contain("{{customerName}}").And.Contain("{{appointmentTime}}");
    }

    // ── El job ────────────────────────────────────────────────────────────

    [Fact]
    public async Task El_job_fija_el_centro_y_a_un_aviso_que_aun_no_toca_no_le_hace_nada()
    {
        var center = await CenterAsync();
        var id = await BookAsync(center, new TimeOnly(10, 0));
        await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/confirm", center.Org, center.Token);

        // La cita es en 2031: a ningún aviso le ha llegado la hora.
        await RunJobAsync(center.Org, id, center.DayBefore.Id, ReminderChannels.Email);

        (await LogsAsync(center.Org, id)).Should().HaveCount(3).And.OnlyContain(l => l.Status == "pending");
    }

    [Fact]
    public async Task Si_la_cita_se_cancela_el_job_descarta_su_aviso_pendiente()
    {
        var center = await CenterAsync();
        var id = await BookAsync(center, new TimeOnly(10, 0));
        await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/confirm", center.Org, center.Token);
        (await factory.SendAsync(
            HttpMethod.Post, $"{Appointments}/{id}/cancel", center.Org, center.Token, new { reason = "Prueba" }))
            .Status.Should().Be(HttpStatusCode.OK);

        await RunJobAsync(center.Org, id, center.DayBefore.Id, ReminderChannels.Email);

        (await LogsAsync(center.Org, id)).Select(l => l.ReminderConfigurationId)
            .Should().Equal(center.SameDay.Id, center.SameDay.Id);
    }

    [Fact]
    public async Task Un_job_con_el_centro_equivocado_no_ve_la_cita_ni_toca_sus_avisos()
    {
        var center = await CenterAsync();
        var other = await CenterAsync();
        var id = await BookAsync(center, new TimeOnly(10, 0));
        await factory.SendAsync(HttpMethod.Post, $"{Appointments}/{id}/confirm", center.Org, center.Token);

        // Mismo id de cita, otro centro: para ese centro la cita no existe.
        await RunJobAsync(other.Org, id, center.DayBefore.Id, ReminderChannels.Email);

        (await LogsAsync(center.Org, id)).Should().HaveCount(3);
        (await LogsAsync(other.Org, id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Un_aviso_al_que_ya_le_toca_queda_listo_y_pendiente_hasta_que_exista_el_envio()
    {
        var center = await CenterAsync();
        // Cita confirmada dentro de una semana con un recordatorio de 1000 horas
        // (unos 41 días): su hora de envío ya pasó.
        var employee = await factory.CreateEmployeeAsync(center.Org);
        var customer = await factory.CreateCustomerAsync(center.Org);
        var soon = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var appointment = await factory.CreateAppointmentAsync(
            center.Org, employee, customer, soon, new TimeOnly(10, 0), new TimeOnly(11, 0), AppointmentStatuses.Confirmed);
        var overdue = await AddConfigurationAsync(center.Org, center.DayBefore.MessageTemplateId, order: 3, hours: 1000);

        ReminderDueOutcome outcome;
        await using (var scope = factory.CreateTenantScope(center.Org))
        {
            outcome = await scope.ServiceProvider.GetRequiredService<IReminderService>()
                .ProcessDueAsync(appointment.Id, overdue.Id, ReminderChannels.Email);
        }

        outcome.Value.Should().Be(ReminderDueOutcomes.Ready);
        var log = (await LogsAsync(center.Org, appointment.Id)).Should().ContainSingle().Subject;
        log.Id.Should().Be(outcome.ReminderLogId);
        log.Status.Should().Be(ReminderLogStatuses.Pending);
    }

    // ── Andamiaje ─────────────────────────────────────────────────────────

    private sealed record Center(
        Guid Org, string Token, AppointmentScene Scene, ReminderConfiguration DayBefore, ReminderConfiguration SameDay);

    /// <summary>
    /// Centro nuevo (hora peninsular) con dos recordatorios: 24 h antes por email
    /// sin franja, y 2 h antes por email y WhatsApp entre las 09:00 y las 21:00.
    /// </summary>
    private async Task<Center> CenterAsync()
    {
        var org = await factory.CreateOrganizationAsync();
        var scene = await factory.CreateAppointmentSceneAsync(org);
        var token = await factory.TokenForAsync(org, scene.Employee.Id);

        int templateId;
        await using (var scope = factory.CreateTenantScope(org))
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var template = new MessageTemplate
            {
                OrganizationId = org,
                Name = "Recordatorio",
                Subject = "Tu cita",
                Body = "Hola, {{customerName}}.",
            };
            db.MessageTemplates.Add(template);
            await db.SaveChangesAsync();
            templateId = template.Id;
        }

        var dayBefore = await AddConfigurationAsync(org, templateId, order: 1, hours: 24);
        var sameDay = await AddConfigurationAsync(
            org, templateId, order: 2, hours: 2, ReminderChannels.Both, new TimeOnly(9, 0), new TimeOnly(21, 0));

        return new Center(org, token, scene, dayBefore, sameDay);
    }

    private async Task<ReminderConfiguration> AddConfigurationAsync(
        Guid org,
        int templateId,
        int order,
        int hours,
        string channel = ReminderChannels.Email,
        TimeOnly? windowStart = null,
        TimeOnly? windowEnd = null)
    {
        await using var scope = factory.CreateTenantScope(org);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = new ReminderConfiguration
        {
            OrganizationId = org,
            ReminderOrder = order,
            HoursBeforeAppointment = hours,
            Channel = channel,
            MessageTemplateId = templateId,
            AllowedSendStartTime = windowStart,
            AllowedSendEndTime = windowEnd,
        };
        db.ReminderConfigurations.Add(configuration);
        await db.SaveChangesAsync();
        return configuration;
    }

    /// <summary>Alta de una cita de tinte (30 min) el día <see cref="Day"/> por la API; devuelve su id.</summary>
    private async Task<int> BookAsync(Center center, TimeOnly start)
    {
        var created = await factory.SendAsync(
            HttpMethod.Post, Appointments, center.Org, center.Token, center.Scene.Tinting(start));
        created.Status.Should().Be(HttpStatusCode.Created);
        return created.Data.GetProperty("id").GetInt32();
    }

    private async Task<List<ReminderLog>> LogsAsync(Guid org, int appointmentId)
    {
        await using var scope = factory.CreateTenantScope(org);
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().ReminderLogs
            .AsNoTracking()
            .Where(l => l.AppointmentId == appointmentId)
            .OrderBy(l => l.ReminderConfigurationId).ThenBy(l => l.Channel)
            .ToListAsync();
    }

    /// <summary>Ejecuta el job como lo haría Hangfire: en un scope sin petición ni tenant.</summary>
    private async Task RunJobAsync(Guid org, int appointmentId, int configurationId, string channel)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ReminderJob>()
            .RunAsync(org, appointmentId, configurationId, channel);
    }
}
