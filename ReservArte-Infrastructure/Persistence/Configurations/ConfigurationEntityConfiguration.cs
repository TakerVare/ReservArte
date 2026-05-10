using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class ConfigurationEntityConfiguration : IEntityTypeConfiguration<Domain.Entities.Configuration>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Configuration> builder)
    {
        builder.ToTable("Configuration");
        builder.HasKey(e => e.Id);

        // Id fijo = 1; EF nunca debe intentar generarlo
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CompanyName).HasMaxLength(150);
        builder.Property(e => e.CompanyLogo).HasMaxLength(250);
    }
}
