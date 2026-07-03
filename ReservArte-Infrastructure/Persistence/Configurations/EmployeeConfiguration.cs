using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever(); // Id = User.Id (patrón del esquema real)
        builder.Property(e => e.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(e => e.Email).IsUnique();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Rol).HasMaxLength(50);
        builder.Property(e => e.ProfileImageUrl).HasMaxLength(500);

        builder.HasOne(e => e.Organization)
               .WithMany()
               .HasForeignKey(e => e.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
               .WithOne(u => u.Employee)
               .HasForeignKey<Employee>(e => e.Id)
               .OnDelete(DeleteBehavior.Cascade);
    }
}