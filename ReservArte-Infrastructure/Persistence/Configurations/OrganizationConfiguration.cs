using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        // Ventana de reserva (H-44): 6 y 10 semanas por defecto, entre 1 y 52.
        builder.ToTable("Organizations", t =>
        {
            t.HasCheckConstraint(
                "CK_Organizations_CustomerBookingWindowWeeks",
                "\"CustomerBookingWindowWeeks\" BETWEEN 1 AND 52");
            t.HasCheckConstraint(
                "CK_Organizations_StaffBookingWindowWeeks",
                "\"StaffBookingWindowWeeks\" BETWEEN 1 AND 52");
        });
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

        builder.Property(o => o.CustomerBookingWindowWeeks).HasDefaultValue(6);
        builder.Property(o => o.StaffBookingWindowWeeks).HasDefaultValue(10);
    }
}
