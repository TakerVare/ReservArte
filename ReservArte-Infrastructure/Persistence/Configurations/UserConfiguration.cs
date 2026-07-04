using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).UseIdentityColumn(); // INT IDENTITY(1,1), como el esquema SQL

        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Password).HasMaxLength(255).IsRequired(); // v2: hash BCrypt
        builder.Property(u => u.Rol).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.ProfileImageUrl).HasMaxLength(500);

        builder.HasOne(u => u.Organization)
               .WithMany(o => o.Users)
               .HasForeignKey(u => u.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict); // evita rutas de cascada múltiples en SQL Server
    }
}