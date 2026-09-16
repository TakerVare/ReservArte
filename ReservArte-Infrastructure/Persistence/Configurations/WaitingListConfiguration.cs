using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class WaitingListConfiguration : IEntityTypeConfiguration<WaitingList>
{
    public void Configure(EntityTypeBuilder<WaitingList> builder)
    {
        // Plural como el resto del esquema, aunque el ERD de diseño la nombre en
        // singular: cada fila es una entrada de la lista, no una lista. Decisión
        // del usuario al revisar RA-869d7f4j8, ya con la tabla creada, así que
        // el renombrado va en su propia migración.
        builder.ToTable("WaitingLists", t =>
            // Un rango invertido no lo podría satisfacer ningún hueco.
            t.HasCheckConstraint(
                "CK_WaitingLists_DateRange",
                "[DateRangeEnd] > [DateRangeStart]"));

        builder.HasKey(w => w.Id);

        // Se recorre por servicio y en orden de atención: menor va antes.
        builder.HasIndex(w => new { w.OrganizationId, w.ServiceId, w.Priority })
               .HasDatabaseName("idx_waiting_lists_org_service_priority");

        builder.HasIndex(w => w.CustomerId);

        // Cascade desde la ficha del cliente: apuntarse a la espera es un dato
        // suyo, no histórico de negocio como una cita (mismo criterio que
        // CustomerNotes, CustomerAllergies y CustomerConsents).
        builder.HasOne(w => w.Customer)
               .WithMany()
               .HasForeignKey(w => w.CustomerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.Service)
               .WithMany()
               .HasForeignKey(w => w.ServiceId)
               .OnDelete(DeleteBehavior.Restrict);

        // Restrict, además, porque es el segundo camino que llega hasta aquí
        // desde AspNetUsers (vía Customers y vía Employees), igual que en
        // CustomerNotes.EmployeeId.
        builder.HasOne(w => w.PreferredEmployee)
               .WithMany()
               .HasForeignKey(w => w.PreferredEmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Organization)
               .WithMany()
               .HasForeignKey(w => w.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
