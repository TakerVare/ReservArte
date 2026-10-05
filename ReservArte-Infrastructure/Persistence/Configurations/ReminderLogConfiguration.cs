using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class ReminderLogConfiguration : IEntityTypeConfiguration<ReminderLog>
{
    public void Configure(EntityTypeBuilder<ReminderLog> builder)
    {
        builder.ToTable("ReminderLogs", t =>
        {
            // Un envío sale por un canal concreto: «both» solo existe en la configuración.
            t.HasCheckConstraint(
                "CK_ReminderLogs_Channel",
                CatalogCheck.In(nameof(ReminderLog.Channel), ReminderChannels.Single));

            t.HasCheckConstraint(
                "CK_ReminderLogs_Status",
                CatalogCheck.In(nameof(ReminderLog.Status), ReminderLogStatuses.All));
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Channel).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Status).HasMaxLength(20).IsRequired();
        builder.Property(l => l.ExternalMessageId).HasMaxLength(200);
        builder.Property(l => l.ErrorMessage).HasMaxLength(1000);

        // Un envío por cita, recordatorio y canal: si el job se repite, choca
        // aquí en vez de mandar el aviso dos veces.
        // Nombre propio: el generado pasa de los 63 caracteres de PostgreSQL.
        builder.HasIndex(l => new { l.OrganizationId, l.AppointmentId, l.ReminderConfigurationId, l.Channel })
               .IsUnique()
               .HasDatabaseName("IX_ReminderLogs_Organization_Appointment_Configuration_Channel");

        // Los envíos de una cita (la ficha) y el de un recordatorio concreto (el job).
        builder.HasIndex(l => new { l.AppointmentId, l.ReminderConfigurationId });

        // El proveedor avisa de entregas y aperturas con su identificador.
        builder.HasIndex(l => l.ExternalMessageId)
               .HasFilter("\"ExternalMessageId\" IS NOT NULL");

        // Cascade desde la cita: el envío es un dato suyo. Las citas no se
        // borran en el producto (baja lógica), así que solo aplica a limpiezas.
        builder.HasOne(l => l.Appointment)
               .WithMany()
               .HasForeignKey(l => l.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);

        // Restrict: un recordatorio con envíos no se borra; se desactiva.
        builder.HasOne(l => l.ReminderConfiguration)
               .WithMany()
               .HasForeignKey(l => l.ReminderConfigurationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Organization)
               .WithMany()
               .HasForeignKey(l => l.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
