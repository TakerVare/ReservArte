using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class EmployeeExceptionConfiguration : IEntityTypeConfiguration<EmployeeException>
{
    public void Configure(EntityTypeBuilder<EmployeeException> builder)
    {
        builder.ToTable("EmployeeExceptions", t =>
        {
            // Espejo del CHECK del esquema: los valores viven en
            // EmployeeExceptionTypes (ReservArte-Domain).
            t.HasCheckConstraint(
                "CK_EmployeeExceptions_Type",
                "[Type] IN ('vacation', 'sick_leave', 'personal', 'training', 'other')");

            // Un intervalo invertido no es una ausencia: es un dato corrupto.
            t.HasCheckConstraint(
                "CK_EmployeeExceptions_Interval",
                "[EndDateTime] > [StartDateTime]");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);

        // Las ausencias se buscan por empleado y rango de fechas.
        builder.HasIndex(x => new { x.EmployeeId, x.StartDateTime, x.EndDateTime });
        builder.HasIndex(x => x.OrganizationId);

        builder.HasOne(x => x.Employee)
               .WithMany(e => e.Exceptions)
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Organization)
               .WithMany()
               .HasForeignKey(x => x.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
