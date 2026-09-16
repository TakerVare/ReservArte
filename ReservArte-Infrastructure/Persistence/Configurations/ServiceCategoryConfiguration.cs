using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("ServiceCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);

        // Color de la agenda: cabe cualquier notación corta (#RRGGBB o nombre).
        builder.Property(c => c.Color).HasMaxLength(20);

        // Las categorías se listan por organización en su orden de presentación.
        builder.HasIndex(c => new { c.OrganizationId, c.DisplayOrder });

        builder.HasOne(c => c.Organization)
               .WithMany()
               .HasForeignKey(c => c.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
