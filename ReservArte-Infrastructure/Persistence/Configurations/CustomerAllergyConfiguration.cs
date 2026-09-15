using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class CustomerAllergyConfiguration : IEntityTypeConfiguration<CustomerAllergy>
{
    public void Configure(EntityTypeBuilder<CustomerAllergy> builder)
    {
        builder.ToTable("CustomerAllergies", t =>
            t.HasCheckConstraint(
                "CK_CustomerAllergies_Severity",
                CatalogCheck.In(nameof(CustomerAllergy.Severity), AllergySeverities.All)));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AllergyDescription).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Severity).HasMaxLength(20).IsRequired();

        builder.HasIndex(a => a.CustomerId);
        builder.HasIndex(a => a.OrganizationId);

        builder.HasOne(a => a.Customer)
               .WithMany(c => c.Allergies)
               .HasForeignKey(a => a.CustomerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Organization)
               .WithMany()
               .HasForeignKey(a => a.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
