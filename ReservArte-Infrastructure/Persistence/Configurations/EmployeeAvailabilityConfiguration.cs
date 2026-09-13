using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class EmployeeAvailabilityConfiguration : IEntityTypeConfiguration<EmployeeAvailability>
{
    public void Configure(EntityTypeBuilder<EmployeeAvailability> builder)
    {
        builder.ToTable("EmployeeAvailabilities", t =>
            // Convención del proyecto: 0 = lunes … 6 = domingo (RA-869d7ezrr).
            t.HasCheckConstraint(
                "CK_EmployeeAvailabilities_DayOfWeek",
                "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.StartTime).HasColumnType("time");
        builder.Property(a => a.EndTime).HasColumnType("time");

        // El horario se consulta siempre por empleado y día: este índice cubre
        // el acceso del futuro AvailabilityService.
        builder.HasIndex(a => new { a.EmployeeId, a.DayOfWeek });
        builder.HasIndex(a => a.OrganizationId);

        builder.HasOne(a => a.Employee)
               .WithMany(e => e.Availabilities)
               .HasForeignKey(a => a.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        // Restrict y no Cascade: borrar una organización con datos debe fallar,
        // no llevarse por delante el histórico (mismo criterio que Employee).
        builder.HasOne(a => a.Organization)
               .WithMany()
               .HasForeignKey(a => a.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
