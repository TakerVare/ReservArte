using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services", t =>
            // Una duración de 0 o negativa haría que la cita no ocupase hueco en
            // la agenda; un precio negativo, que el importe restase.
            t.HasCheckConstraint(
                "CK_Services_DurationAndPrice",
                "[DurationMinutes] > 0 AND [BasePrice] >= 0"));

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.ImageUrl).HasMaxLength(500);
        builder.Property(s => s.BasePrice).HasColumnType("decimal(10,2)");

        // El catálogo se lista por organización y se filtra por categoría.
        builder.HasIndex(s => s.OrganizationId);
        builder.HasIndex(s => s.CategoryId);

        // Restrict: una categoría con servicios no se borra de debajo. La baja
        // de categoría es lógica (IsActive), como en el resto del catálogo.
        builder.HasOne(s => s.Category)
               .WithMany(c => c.Services)
               .HasForeignKey(s => s.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Organization)
               .WithMany()
               .HasForeignKey(s => s.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
