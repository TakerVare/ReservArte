using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class ServiceVariationConfiguration : IEntityTypeConfiguration<ServiceVariation>
{
    public void Configure(EntityTypeBuilder<ServiceVariation> builder)
    {
        builder.ToTable("ServiceVariations");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name).HasMaxLength(100).IsRequired();

        // Modificador, no precio: puede ser negativo para abaratar la variante.
        builder.Property(v => v.PriceModifier).HasColumnType("decimal(10,2)");

        builder.HasIndex(v => v.ServiceId);
        builder.HasIndex(v => v.OrganizationId);

        builder.HasOne(v => v.Service)
               .WithMany(s => s.Variations)
               .HasForeignKey(v => v.ServiceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Organization)
               .WithMany()
               .HasForeignKey(v => v.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
