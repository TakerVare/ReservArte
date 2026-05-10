using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class EmployeeServiceConfiguration : IEntityTypeConfiguration<EmployeeService>
{
    public void Configure(EntityTypeBuilder<EmployeeService> builder)
    {
        builder.ToTable("EmployeeServices");

        // PK compuesta: (EmployeeId, ServiceId)
        builder.HasKey(e => new { e.EmployeeId, e.ServiceId });

        builder.HasOne(e => e.Employee)
               .WithMany(emp => emp.Services)
               .HasForeignKey(e => e.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Service)
               .WithMany(s => s.Employees)
               .HasForeignKey(e => e.ServiceId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
