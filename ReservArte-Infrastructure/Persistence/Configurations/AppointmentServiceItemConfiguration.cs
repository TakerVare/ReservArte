using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class AppointmentServiceItemConfiguration : IEntityTypeConfiguration<AppointmentServiceItem>
{
    public void Configure(EntityTypeBuilder<AppointmentServiceItem> builder)
    {
        builder.ToTable("AppointmentServiceItems", t =>
            // Precio y duración se congelan al reservar: son los que se cobran y
            // los que ocupan agenda, no los del catálogo de hoy.
            t.HasCheckConstraint(
                "CK_AppointmentServiceItems_PriceAndDuration",
                "[Price] >= 0 AND [DurationMinutes] > 0"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Price).HasColumnType("decimal(10,2)");

        // Las líneas se leen por cita y en su orden de prestación.
        builder.HasIndex(i => new { i.AppointmentId, i.Order });
        builder.HasIndex(i => i.ServiceId);
        builder.HasIndex(i => i.OrganizationId);

        // Cascade: la línea no es nada sin su cita, igual que ServicePackageItem
        // con su paquete.
        builder.HasOne(i => i.Appointment)
               .WithMany(a => a.ServiceItems)
               .HasForeignKey(i => i.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);

        // Restrict: borrar un servicio no puede vaciar lo que ya se prestó (la
        // baja del catálogo es lógica justo por esto), y evita el segundo camino
        // en cascada hasta esta tabla desde Organizations.
        builder.HasOne(i => i.Service)
               .WithMany()
               .HasForeignKey(i => i.ServiceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ServiceVariation)
               .WithMany()
               .HasForeignKey(i => i.ServiceVariationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Organization)
               .WithMany()
               .HasForeignKey(i => i.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
