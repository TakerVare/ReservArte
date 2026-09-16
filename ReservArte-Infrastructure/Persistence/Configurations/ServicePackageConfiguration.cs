using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class ServicePackageConfiguration : IEntityTypeConfiguration<ServicePackage>
{
    public void Configure(EntityTypeBuilder<ServicePackage> builder)
    {
        builder.ToTable("ServicePackages", t =>
        {
            t.HasCheckConstraint("CK_ServicePackages_TotalPrice", "[TotalPrice] >= 0");

            // Es un tanto por ciento: fuera de 0-100 no significa nada.
            t.HasCheckConstraint(
                "CK_ServicePackages_DiscountPercentage",
                "[DiscountPercentage] >= 0 AND [DiscountPercentage] <= 100");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.TotalPrice).HasColumnType("decimal(10,2)");

        // Informativo para la ficha; lo que se cobra es TotalPrice.
        builder.Property(p => p.DiscountPercentage).HasColumnType("decimal(5,2)");

        builder.HasIndex(p => p.OrganizationId);

        builder.HasOne(p => p.Organization)
               .WithMany()
               .HasForeignKey(p => p.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
