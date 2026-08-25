using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Tabla (AspNetUsers), PK y columnas de Identity (Email, UserName,
        // PasswordHash, SecurityStamp, TwoFactorEnabled...) las mapea la
        // base IdentityUserContext en base.OnModelCreating.
        // Aquí solo los campos y relaciones de negocio.

        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Rol)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.ProfileImageUrl)
            .HasMaxLength(500);
        
        // Consentimiento RGPD (vol. 1 §4.4.1). Versiones como cadenas cortas
        // ("1.0", "2.1"); nullables porque seed, cuentas solo-sociales y
        // usuarios previos no registran consentimiento.
        builder.Property(u => u.AcceptedTermsVersion)
            .HasMaxLength(20);
        builder.Property(u => u.AcceptedPrivacyVersion)
            .HasMaxLength(20);

        // Identity solo crea un índice NO único sobre NormalizedEmail
        // ("EmailIndex"); lo redefinimos como ÚNICO conservando el nombre,
        // manteniendo la regla de email único del esquema original.
        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("EmailIndex");

        builder.HasIndex(u => u.OrganizationId);

        builder.HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}