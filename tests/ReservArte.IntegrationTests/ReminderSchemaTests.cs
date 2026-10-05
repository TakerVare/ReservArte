using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ReservArte.Domain.Entities;
using ReservArte.Infrastructure.Persistence;
using ReservArte.IntegrationTests.Infrastructure;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Esquema de recordatorios (RA-869d7f5wx) contra PostgreSQL: lo que SQLite no
/// reproduce. Los CHECK de catálogo y de rango, los únicos por centro, los
/// borrados en cascada o restringidos y el aislamiento entre centros. Cada test
/// usa un centro propio, porque los únicos son por organización.
/// </summary>
[Collection(ApiCollection.Name)]
public class ReminderSchemaTests(ApiFactory factory)
{
    private static readonly DateOnly Day = AppointmentScene.Day;

    // ── Alta completa ─────────────────────────────────────────────────────

    [Fact]
    public async Task Plantilla_recordatorio_envio_y_token_se_guardan_y_se_leen()
    {
        var scene = await SceneAsync();

        await using var scope = factory.CreateTenantScope(scene.Org);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ReminderLogs.Add(Log(scene, status: ReminderLogStatuses.Sent, sentAt: DateTime.UtcNow, externalId: "ses-123"));
        db.ConfirmationTokens.Add(Token(scene, "tok-" + Guid.NewGuid().ToString("N")));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var configuration = await db.ReminderConfigurations.Include(r => r.MessageTemplate).SingleAsync();
        configuration.Should().BeEquivalentTo(new
        {
            OrganizationId = scene.Org,
            ReminderOrder = 1,
            HoursBeforeAppointment = 24,
            Channel = ReminderChannels.Email,
            IsActive = true,
            AllowedSendStartTime = new TimeOnly(9, 0),
            AllowedSendEndTime = new TimeOnly(21, 0),
        });
        configuration.MessageTemplate.Body.Should().Contain("{{customerName}}");
        configuration.MessageTemplate.Language.Should().Be("es");

        var log = await db.ReminderLogs.SingleAsync();
        log.Status.Should().Be(ReminderLogStatuses.Sent);
        log.SentAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        log.ExternalMessageId.Should().Be("ses-123");

        (await db.ConfirmationTokens.SingleAsync()).Should().BeEquivalentTo(
            new { AppointmentId = scene.Appointment.Id, Action = ConfirmationTokenActions.Confirm, UsedAt = (DateTime?)null });
    }

    // ── CHECK ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("sms", 24, 1, "CK_ReminderConfigurations_Channel")]
    [InlineData("email", 0, 1, "CK_ReminderConfigurations_HoursBeforeAppointment")]
    [InlineData("email", 24, 0, "CK_ReminderConfigurations_ReminderOrder")]
    public async Task Un_recordatorio_con_canal_antelacion_u_orden_invalidos_se_rechaza(
        string channel, int hours, int order, string constraint)
    {
        var scene = await SceneAsync();

        await ShouldViolateAsync(scene.Org, PostgresErrorCodes.CheckViolation, constraint, db =>
            db.ReminderConfigurations.Add(new ReminderConfiguration
            {
                OrganizationId = scene.Org,
                ReminderOrder = order == 0 ? 0 : 2,
                HoursBeforeAppointment = hours,
                Channel = channel,
                MessageTemplateId = scene.Template.Id,
            }));
    }

    [Theory]
    [InlineData(9, null)]
    [InlineData(null, 21)]
    [InlineData(21, 9)]
    [InlineData(9, 9)]
    public async Task La_franja_de_envio_va_con_sus_dos_horas_y_en_orden(int? startHour, int? endHour)
    {
        var scene = await SceneAsync();

        await ShouldViolateAsync(
            scene.Org, PostgresErrorCodes.CheckViolation, "CK_ReminderConfigurations_SendWindow", db =>
                db.ReminderConfigurations.Add(new ReminderConfiguration
                {
                    OrganizationId = scene.Org,
                    ReminderOrder = 2,
                    HoursBeforeAppointment = 2,
                    MessageTemplateId = scene.Template.Id,
                    AllowedSendStartTime = startHour is { } s ? new TimeOnly(s, 0) : null,
                    AllowedSendEndTime = endHour is { } e ? new TimeOnly(e, 0) : null,
                }));
    }

    [Fact]
    public async Task Una_plantilla_de_tipo_desconocido_se_rechaza()
    {
        var org = await factory.CreateOrganizationAsync();

        await ShouldViolateAsync(org, PostgresErrorCodes.CheckViolation, "CK_MessageTemplates_Type", db =>
            db.MessageTemplates.Add(Template(org, "Rara", type: "sms_reminder")));
    }

    [Theory]
    [InlineData("both", "pending", "CK_ReminderLogs_Channel")]
    [InlineData("email", "bounced", "CK_ReminderLogs_Status")]
    public async Task Un_envio_va_por_un_solo_canal_y_con_un_estado_del_catalogo(
        string channel, string status, string constraint)
    {
        var scene = await SceneAsync();

        await ShouldViolateAsync(scene.Org, PostgresErrorCodes.CheckViolation, constraint, db =>
            db.ReminderLogs.Add(Log(scene, channel, status)));
    }

    [Fact]
    public async Task Un_token_con_una_accion_desconocida_se_rechaza()
    {
        var scene = await SceneAsync();

        await ShouldViolateAsync(scene.Org, PostgresErrorCodes.CheckViolation, "CK_ConfirmationTokens_Action", db =>
            db.ConfirmationTokens.Add(Token(scene, "tok-" + Guid.NewGuid().ToString("N"), "reschedule")));
    }

    // ── Únicos ────────────────────────────────────────────────────────────

    [Fact]
    public async Task No_hay_dos_envios_de_la_misma_cita_recordatorio_y_canal_pero_si_uno_por_canal()
    {
        var scene = await SceneAsync();
        await SaveAsync(scene.Org, db =>
        {
            db.ReminderLogs.Add(Log(scene));
            db.ReminderLogs.Add(Log(scene, ReminderChannels.WhatsApp));
        });

        // Lo que impide mandar dos veces el mismo aviso si el job se repite.
        await ShouldViolateAsync(
            scene.Org,
            PostgresErrorCodes.UniqueViolation,
            "IX_ReminderLogs_Organization_Appointment_Configuration_Channel",
            db => db.ReminderLogs.Add(Log(scene)));
    }

    [Fact]
    public async Task El_orden_es_unico_entre_los_recordatorios_vigentes_del_centro()
    {
        var scene = await SceneAsync();
        ReminderConfiguration Duplicate() => new()
        {
            OrganizationId = scene.Org,
            ReminderOrder = 1,
            HoursBeforeAppointment = 2,
            MessageTemplateId = scene.Template.Id,
        };

        await ShouldViolateAsync(
            scene.Org,
            PostgresErrorCodes.UniqueViolation,
            "IX_ReminderConfigurations_OrganizationId_ReminderOrder",
            db => db.ReminderConfigurations.Add(Duplicate()));

        // Desactivado el primero, su posición queda libre.
        await SaveAsync(scene.Org, async db =>
            (await db.ReminderConfigurations.SingleAsync(r => r.Id == scene.Configuration.Id)).IsActive = false);
        await SaveAsync(scene.Org, db => db.ReminderConfigurations.Add(Duplicate()));

        // Otro centro usa el mismo orden sin chocar.
        var other = await SceneAsync();
        other.Configuration.ReminderOrder.Should().Be(1);
    }

    [Fact]
    public async Task El_nombre_de_plantilla_es_unico_entre_las_vigentes_del_centro()
    {
        var scene = await SceneAsync();

        await ShouldViolateAsync(
            scene.Org,
            PostgresErrorCodes.UniqueViolation,
            "IX_MessageTemplates_OrganizationId_Name",
            db => db.MessageTemplates.Add(Template(scene.Org, scene.Template.Name)));
    }

    [Fact]
    public async Task El_mismo_token_no_se_guarda_dos_veces()
    {
        var scene = await SceneAsync();
        var token = "tok-" + Guid.NewGuid().ToString("N");
        await SaveAsync(scene.Org, db => db.ConfirmationTokens.Add(Token(scene, token)));

        await ShouldViolateAsync(scene.Org, PostgresErrorCodes.UniqueViolation, "PK_ConfirmationTokens", db =>
            db.ConfirmationTokens.Add(Token(scene, token, ConfirmationTokenActions.Cancel)));
    }

    // ── Borrados ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Borrar_una_cita_se_lleva_sus_envios_y_sus_tokens()
    {
        var scene = await SceneAsync();
        await SaveAsync(scene.Org, db =>
        {
            db.ReminderLogs.Add(Log(scene));
            db.ConfirmationTokens.Add(Token(scene, "tok-" + Guid.NewGuid().ToString("N")));
        });

        await using var scope = factory.CreateTenantScope(scene.Org);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Appointments.Where(a => a.Id == scene.Appointment.Id).ExecuteDeleteAsync();

        (await db.ReminderLogs.CountAsync()).Should().Be(0);
        (await db.ConfirmationTokens.CountAsync()).Should().Be(0);
        (await db.ReminderConfigurations.CountAsync()).Should().Be(1, "el recordatorio es del centro, no de la cita");
    }

    [Fact]
    public async Task No_se_borra_una_plantilla_en_uso_ni_un_recordatorio_con_envios()
    {
        var scene = await SceneAsync();
        await SaveAsync(scene.Org, db => db.ReminderLogs.Add(Log(scene)));

        await using var scope = factory.CreateTenantScope(scene.Org);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // PostgreSQL distingue el RESTRICT (23001) de la FK sin acción (23503).
        var template = () => db.MessageTemplates.Where(m => m.Id == scene.Template.Id).ExecuteDeleteAsync();
        (await template.Should().ThrowAsync<PostgresException>())
            .Which.SqlState.Should().Be(PostgresErrorCodes.RestrictViolation);

        var configuration = () =>
            db.ReminderConfigurations.Where(r => r.Id == scene.Configuration.Id).ExecuteDeleteAsync();
        (await configuration.Should().ThrowAsync<PostgresException>())
            .Which.SqlState.Should().Be(PostgresErrorCodes.RestrictViolation);
    }

    // ── Aislamiento ───────────────────────────────────────────────────────

    [Fact]
    public async Task Un_centro_no_ve_los_recordatorios_de_otro_y_sin_centro_no_se_ve_ninguno()
    {
        var one = await SceneAsync();
        var other = await SceneAsync();
        var token = "tok-" + Guid.NewGuid().ToString("N");
        await SaveAsync(one.Org, db =>
        {
            db.ReminderLogs.Add(Log(one));
            db.ConfirmationTokens.Add(Token(one, token));
        });

        await using (var scope = factory.CreateTenantScope(other.Org))
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.MessageTemplates.Select(m => m.Id).ToListAsync()).Should().Equal(other.Template.Id);
            (await db.ReminderConfigurations.Select(r => r.Id).ToListAsync()).Should().Equal(other.Configuration.Id);
            (await db.ReminderLogs.CountAsync()).Should().Be(0);
            (await db.ConfirmationTokens.FindAsync(token)).Should().BeNull("el token es de otro centro");
        }

        await using var noTenant = factory.Services.CreateAsyncScope();
        var closed = noTenant.ServiceProvider.GetRequiredService<AppDbContext>();
        (await closed.MessageTemplates.CountAsync()).Should().Be(0);
        (await closed.ReminderConfigurations.CountAsync()).Should().Be(0);
        (await closed.ReminderLogs.CountAsync()).Should().Be(0);
        (await closed.ConfirmationTokens.CountAsync()).Should().Be(0);
    }

    // ── Andamiaje ─────────────────────────────────────────────────────────

    private sealed record Scene(
        Guid Org, MessageTemplate Template, ReminderConfiguration Configuration, Appointment Appointment);

    /// <summary>Centro nuevo con una plantilla, un recordatorio a 24 h por email y una cita.</summary>
    private async Task<Scene> SceneAsync()
    {
        var org = await factory.CreateOrganizationAsync();
        var employee = await factory.CreateEmployeeAsync(org);
        var customer = await factory.CreateCustomerAsync(org);
        var appointment = await factory.CreateAppointmentAsync(
            org, employee, customer, Day, new TimeOnly(10, 0), new TimeOnly(11, 0));

        var template = Template(org, "Recordatorio 24 h");
        await SaveAsync(org, db => db.MessageTemplates.Add(template));

        var configuration = new ReminderConfiguration
        {
            OrganizationId = org,
            ReminderOrder = 1,
            HoursBeforeAppointment = 24,
            MessageTemplateId = template.Id,
            AllowedSendStartTime = new TimeOnly(9, 0),
            AllowedSendEndTime = new TimeOnly(21, 0),
        };
        await SaveAsync(org, db => db.ReminderConfigurations.Add(configuration));

        return new Scene(org, template, configuration, appointment);
    }

    private static MessageTemplate Template(
        Guid org, string name, string type = MessageTemplateTypes.EmailReminder) => new()
        {
            OrganizationId = org,
            Name = name,
            Type = type,
            Subject = "Tu cita en {{organizationName}}",
            Body = "Hola, {{customerName}}: te esperamos el {{appointmentDate}} a las {{appointmentTime}}.",
        };

    private static ReminderLog Log(
        Scene scene,
        string channel = ReminderChannels.Email,
        string status = ReminderLogStatuses.Pending,
        DateTime? sentAt = null,
        string? externalId = null) => new()
        {
            OrganizationId = scene.Org,
            AppointmentId = scene.Appointment.Id,
            ReminderConfigurationId = scene.Configuration.Id,
            Channel = channel,
            Status = status,
            SentAt = sentAt,
            ExternalMessageId = externalId,
        };

    private static ConfirmationToken Token(
        Scene scene, string token, string action = ConfirmationTokenActions.Confirm) => new()
        {
            Token = token,
            OrganizationId = scene.Org,
            AppointmentId = scene.Appointment.Id,
            Action = action,
            ExpiresAt = DateTime.UtcNow.AddDays(2),
        };

    private Task SaveAsync(Guid org, Action<AppDbContext> change) =>
        SaveAsync(org, db =>
        {
            change(db);
            return Task.CompletedTask;
        });

    private async Task SaveAsync(Guid org, Func<AppDbContext, Task> change)
    {
        await using var scope = factory.CreateTenantScope(org);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await change(db);
        await db.SaveChangesAsync();
    }

    private async Task ShouldViolateAsync(Guid org, string sqlState, string constraint, Action<AppDbContext> change)
    {
        var act = () => SaveAsync(org, change);

        var error = (await act.Should().ThrowAsync<DbUpdateException>()).Which.InnerException
            .Should().BeOfType<PostgresException>().Which;
        error.SqlState.Should().Be(sqlState);
        error.ConstraintName.Should().Be(constraint);
    }
}
