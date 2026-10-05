using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class OrganizationSettingsConfiguration : IEntityTypeConfiguration<OrganizationSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationSettings> builder)
    {
        // Mismos límites que el validador: los CHECK cierran el paso al SQL a mano.
        builder.ToTable("OrganizationSettings", t =>
        {
            t.HasCheckConstraint(
                "CK_OrganizationSettings_CancellationHoursThreshold",
                $"\"CancellationHoursThreshold\" BETWEEN {OrganizationSettings.MinCancellationHoursThreshold} AND {OrganizationSettings.MaxCancellationHoursThreshold}");
            t.HasCheckConstraint(
                "CK_OrganizationSettings_MaxNoShowsBeforeBlock",
                $"\"MaxNoShowsBeforeBlock\" BETWEEN {OrganizationSettings.MinMaxNoShowsBeforeBlock} AND {OrganizationSettings.MaxMaxNoShowsBeforeBlock}");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TimeZone)
               .HasMaxLength(OrganizationSettings.TimeZoneMaxLength)
               .HasDefaultValue(OrganizationSettings.DefaultTimeZone)
               .IsRequired();
        builder.Property(s => s.CancellationHoursThreshold)
               .HasDefaultValue(OrganizationSettings.DefaultCancellationHoursThreshold);
        builder.Property(s => s.MaxNoShowsBeforeBlock)
               .HasDefaultValue(OrganizationSettings.DefaultMaxNoShowsBeforeBlock);

        // Una fila por centro.
        builder.HasIndex(s => s.OrganizationId).IsUnique();

        builder.HasOne(s => s.Organization)
               .WithMany()
               .HasForeignKey(s => s.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
