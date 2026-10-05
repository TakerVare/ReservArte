using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class ConfirmationTokenConfiguration : IEntityTypeConfiguration<ConfirmationToken>
{
    public void Configure(EntityTypeBuilder<ConfirmationToken> builder)
    {
        builder.ToTable("ConfirmationTokens", t =>
            t.HasCheckConstraint(
                "CK_ConfirmationTokens_Action",
                CatalogCheck.In(nameof(ConfirmationToken.Action), ConfirmationTokenActions.All)));

        // El token es la clave: se busca siempre por él.
        builder.HasKey(c => c.Token);

        builder.Property(c => c.Token).HasMaxLength(ConfirmationToken.TokenMaxLength);
        builder.Property(c => c.Action).HasMaxLength(10).IsRequired();

        builder.HasIndex(c => c.AppointmentId);
        builder.HasIndex(c => c.OrganizationId);

        // Cascade desde la cita, como sus envíos.
        builder.HasOne(c => c.Appointment)
               .WithMany()
               .HasForeignKey(c => c.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Organization)
               .WithMany()
               .HasForeignKey(c => c.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
