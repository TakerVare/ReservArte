using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever(); // Id = User.Id
        builder.Property(e => e.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(e => e.Email).IsUnique();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Rol).HasMaxLength(50);
        builder.Property(e => e.Category).HasMaxLength(50).HasDefaultValue("regular");
        builder.Property(e => e.BlockedReason).HasMaxLength(500);
        builder.Property(e => e.PreferredContactMethod).HasMaxLength(50).HasDefaultValue("email");

        builder.HasOne(e => e.User)
               .WithOne(u => u.Customer)
               .HasForeignKey<Customer>(e => e.Id)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
