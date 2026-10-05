using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class ReminderConfigurationConfiguration : IEntityTypeConfiguration<ReminderConfiguration>
{
    public void Configure(EntityTypeBuilder<ReminderConfiguration> builder)
    {
        builder.ToTable("ReminderConfigurations", t =>
        {
            t.HasCheckConstraint(
                "CK_ReminderConfigurations_Channel",
                CatalogCheck.In(nameof(ReminderConfiguration.Channel), ReminderChannels.All));

            t.HasCheckConstraint("CK_ReminderConfigurations_ReminderOrder", "\"ReminderOrder\" >= 1");

            // Un recordatorio a 0 horas, o después de la cita, no avisa de nada.
            t.HasCheckConstraint(
                "CK_ReminderConfigurations_HoursBeforeAppointment",
                "\"HoursBeforeAppointment\" >= 1");

            // Las dos horas o ninguna, y la franja no cruza la medianoche.
            t.HasCheckConstraint(
                "CK_ReminderConfigurations_SendWindow",
                "(\"AllowedSendStartTime\" IS NULL AND \"AllowedSendEndTime\" IS NULL) OR "
                + "(\"AllowedSendStartTime\" IS NOT NULL AND \"AllowedSendEndTime\" IS NOT NULL "
                + "AND \"AllowedSendEndTime\" > \"AllowedSendStartTime\")");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Channel).HasMaxLength(20).IsRequired();
        builder.Property(r => r.AllowedSendStartTime).HasColumnType("time");
        builder.Property(r => r.AllowedSendEndTime).HasColumnType("time");

        // Una posición por recordatorio vigente del centro.
        builder.HasIndex(r => new { r.OrganizationId, r.ReminderOrder })
               .IsUnique()
               .HasFilter("\"IsActive\" = TRUE");

        // Restrict: una plantilla en uso no se borra; se retira (baja lógica).
        builder.HasOne(r => r.MessageTemplate)
               .WithMany()
               .HasForeignKey(r => r.MessageTemplateId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Organization)
               .WithMany()
               .HasForeignKey(r => r.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
