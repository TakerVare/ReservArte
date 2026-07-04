using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Subdomain).HasMaxLength(100).IsRequired();
        builder.HasIndex(o => o.Subdomain).IsUnique();
        builder.Property(o => o.Email).HasMaxLength(255).IsRequired();
        builder.Property(o => o.Phone).HasMaxLength(20);
        builder.Property(o => o.Address).HasMaxLength(300);
        builder.Property(o => o.City).HasMaxLength(100);
        builder.Property(o => o.PostalCode).HasMaxLength(10);
        builder.Property(o => o.Country).HasMaxLength(2).HasDefaultValue("ES");
        builder.Property(o => o.TaxId).HasMaxLength(20);
        builder.Property(o => o.LogoUrl).HasMaxLength(500);
    }
}