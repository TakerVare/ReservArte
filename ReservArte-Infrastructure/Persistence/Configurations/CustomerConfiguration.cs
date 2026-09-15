using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", t =>
        {
            t.HasCheckConstraint(
                "CK_Customers_Category",
                CatalogCheck.In(nameof(Customer.Category), CustomerCategories.All));

            t.HasCheckConstraint(
                "CK_Customers_PreferredContactMethod",
                CatalogCheck.In(nameof(Customer.PreferredContactMethod), CustomerContactMethods.All));
        });

        builder.HasKey(c => c.Id);

        // Clave primaria compartida: el Id del cliente ES el de su cuenta.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(255).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.ProfileImageUrl).HasMaxLength(500);
        builder.Property(c => c.Category).HasMaxLength(20).IsRequired();
        builder.Property(c => c.BlockedReason).HasMaxLength(500);
        builder.Property(c => c.PreferredContactMethod).HasMaxLength(20).IsRequired();

        // Único por organización, como la cuenta de Identity (RA-869f1xc0u): la
        // misma persona puede ser cliente de varios centros.
        builder.HasIndex(c => new { c.OrganizationId, c.Email }).IsUnique();

        builder.HasOne(c => c.User)
               .WithOne(u => u.Customer)
               .HasForeignKey<Customer>(c => c.Id)
               .OnDelete(DeleteBehavior.Cascade);

        // Restrict: borrar una organización con clientes debe fallar, no
        // llevarse por delante el histórico (mismo criterio que Employee).
        builder.HasOne(c => c.Organization)
               .WithMany()
               .HasForeignKey(c => c.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
