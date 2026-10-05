using AwesomeAssertions;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Entidades de recordatorios (RA-869d7f5wx): valores por defecto y catálogos.
/// El esquema (CHECK, únicos y borrados) se prueba contra PostgreSQL en
/// <c>ReminderSchemaTests</c>.
/// </summary>
public class ReminderDomainTests
{
    [Fact]
    public void Un_envio_nace_pendiente_y_sin_fecha_de_envio()
    {
        var log = new ReminderLog();

        log.Status.Should().Be(ReminderLogStatuses.Pending);
        log.SentAt.Should().BeNull("todavía no ha salido");
        log.Channel.Should().Be(ReminderChannels.Email);
    }

    [Fact]
    public void Un_recordatorio_nace_activo_por_email_y_sin_franja_de_envio()
    {
        var configuration = new ReminderConfiguration();

        configuration.IsActive.Should().BeTrue();
        configuration.Channel.Should().Be(ReminderChannels.Email);
        configuration.AllowedSendStartTime.Should().BeNull();
        configuration.AllowedSendEndTime.Should().BeNull();
    }

    [Fact]
    public void Una_plantilla_nace_activa_y_en_espanol()
    {
        var template = new MessageTemplate();

        template.IsActive.Should().BeTrue();
        template.Language.Should().Be("es");
        template.Type.Should().Be(MessageTemplateTypes.EmailReminder);
    }

    [Fact]
    public void Un_token_nace_sin_usar()
    {
        new ConfirmationToken().UsedAt.Should().BeNull();
    }

    [Fact]
    public void Los_catalogos_son_los_del_diseno()
    {
        ReminderChannels.All.Should().BeEquivalentTo("email", "whatsapp", "both");
        MessageTemplateTypes.All.Should().BeEquivalentTo("email_reminder", "whatsapp_reminder", "confirmation");
        ReminderLogStatuses.All.Should().BeEquivalentTo("pending", "sent", "failed", "delivered", "opened");
        ConfirmationTokenActions.All.Should().BeEquivalentTo("confirm", "cancel");
    }

    [Fact]
    public void Un_envio_concreto_no_puede_ir_por_los_dos_canales()
    {
        // «both» es de la configuración: genera un envío por canal.
        ReminderChannels.Single.Should().BeEquivalentTo("email", "whatsapp");
        ReminderChannels.Single.Should().BeSubsetOf(ReminderChannels.All);
    }

    [Theory]
    [InlineData(typeof(MessageTemplate))]
    [InlineData(typeof(ReminderConfiguration))]
    [InlineData(typeof(ReminderLog))]
    [InlineData(typeof(ConfirmationToken))]
    public void Las_cuatro_llevan_su_propia_organizacion(Type entity)
    {
        // Guid y propio (RA-869f17myx): el query filter no depende de un JOIN.
        entity.GetProperty("OrganizationId")!.PropertyType.Should().Be(typeof(Guid));
    }
}
