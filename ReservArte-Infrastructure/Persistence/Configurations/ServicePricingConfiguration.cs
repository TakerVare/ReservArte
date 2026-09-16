using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class ServicePricingConfiguration : IEntityTypeConfiguration<ServicePricing>
{
    public void Configure(EntityTypeBuilder<ServicePricing> builder)
    {
        builder.ToTable("ServicePricings", t =>
        {
            t.HasCheckConstraint(
                "CK_ServicePricings_EmployeeLevel",
                CatalogCheck.In(nameof(ServicePricing.EmployeeLevel), EmployeeLevels.All));

            t.HasCheckConstraint("CK_ServicePricings_Price", "[Price] >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.EmployeeLevel).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Price).HasColumnType("decimal(10,2)");

        // Una sola tarifa vigente por servicio y nivel: cambiarla es editar esa
        // fila. Las dadas de baja quedan fuera del índice, igual que en
        // CustomerConsents.
        builder.HasIndex(p => new { p.ServiceId, p.EmployeeLevel })
               .IsUnique()
               .HasFilter("[IsActive] = 1");
        builder.HasIndex(p => p.OrganizationId);

        builder.HasOne(p => p.Service)
               .WithMany(s => s.Pricings)
               .HasForeignKey(p => p.ServiceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Organization)
               .WithMany()
               .HasForeignKey(p => p.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
