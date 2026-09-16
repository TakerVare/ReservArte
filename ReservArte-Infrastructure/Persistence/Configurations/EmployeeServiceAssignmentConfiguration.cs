using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

/// <summary>
/// Tabla puente `EmployeeServices`. El nombre de la tabla NO sigue al de la
/// clase a propósito (RA-869f17y7n): la clase se llama ...Assignment para no
/// colisionar con el servicio de aplicación `EmployeeService`.
/// </summary>
public class EmployeeServiceAssignmentConfiguration
    : IEntityTypeConfiguration<EmployeeServiceAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeServiceAssignment> builder)
    {
        builder.ToTable("EmployeeServices", t =>
            t.HasCheckConstraint(
                "CK_EmployeeServices_ProficiencyLevel",
                "[ProficiencyLevel] >= 1 AND [ProficiencyLevel] <= 5"));

        // Clave compuesta: la asignación ES el par empleada-servicio, y así la
        // propia PK impide duplicarla.
        builder.HasKey(a => new { a.EmployeeId, a.ServiceId });

        builder.HasIndex(a => a.ServiceId);
        builder.HasIndex(a => a.OrganizationId);

        builder.HasOne(a => a.Service)
               .WithMany(s => s.Employees)
               .HasForeignKey(a => a.ServiceId)
               .OnDelete(DeleteBehavior.Cascade);

        // Restrict: SQL Server rechaza dos caminos en cascada hasta esta tabla
        // (vía Employees, que cuelga de AspNetUsers, y vía Services). Mismo caso
        // que CustomerNotes.EmployeeId.
        builder.HasOne(a => a.Employee)
               .WithMany(e => e.Services)
               .HasForeignKey(a => a.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Organization)
               .WithMany()
               .HasForeignKey(a => a.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
