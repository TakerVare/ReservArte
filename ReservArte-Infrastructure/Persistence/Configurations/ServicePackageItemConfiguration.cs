using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class ServicePackageItemConfiguration : IEntityTypeConfiguration<ServicePackageItem>
{
    public void Configure(EntityTypeBuilder<ServicePackageItem> builder)
    {
        builder.ToTable("ServicePackageItems");

        builder.HasKey(i => i.Id);

        // Las líneas se leen por paquete y en su orden de prestación.
        builder.HasIndex(i => new { i.ServicePackageId, i.Order });
        builder.HasIndex(i => i.ServiceId);
        builder.HasIndex(i => i.OrganizationId);

        builder.HasOne(i => i.ServicePackage)
               .WithMany(p => p.Items)
               .HasForeignKey(i => i.ServicePackageId)
               .OnDelete(DeleteBehavior.Cascade);

        // Restrict, por dos motivos: borrar un servicio no debe vaciar en
        // silencio los paquetes que lo incluyen, y SQL Server rechaza dos
        // caminos en cascada hasta esta tabla (vía ServicePackages y vía
        // Services, ambos colgando de Organizations).
        builder.HasOne(i => i.Service)
               .WithMany(s => s.PackageItems)
               .HasForeignKey(i => i.ServiceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Organization)
               .WithMany()
               .HasForeignKey(i => i.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
