using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class CustomerConsentConfiguration : IEntityTypeConfiguration<CustomerConsent>
{
    public void Configure(EntityTypeBuilder<CustomerConsent> builder)
    {
        builder.ToTable("CustomerConsents", t =>
        {
            t.HasCheckConstraint(
                "CK_CustomerConsents_ConsentType",
                CatalogCheck.In(nameof(CustomerConsent.ConsentType), CustomerConsentTypes.All));

            // Un consentimiento otorgado sin fecha no se puede demostrar (RGPD).
            t.HasCheckConstraint(
                "CK_CustomerConsents_GrantedAt",
                "[IsGranted] = 0 OR [GrantedAt] IS NOT NULL");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConsentType).HasMaxLength(50).IsRequired();

        // Un único consentimiento vigente por cliente y finalidad: otorgarlo o
        // revocarlo cambia esa fila. Las dadas de baja (IsActive = 0) quedan
        // fuera del índice.
        builder.HasIndex(x => new { x.CustomerId, x.ConsentType })
               .IsUnique()
               .HasFilter("[IsActive] = 1");
        builder.HasIndex(x => x.OrganizationId);

        builder.HasOne(x => x.Customer)
               .WithMany(c => c.Consents)
               .HasForeignKey(x => x.CustomerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Organization)
               .WithMany()
               .HasForeignKey(x => x.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
