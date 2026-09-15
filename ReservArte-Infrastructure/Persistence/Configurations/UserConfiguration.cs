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

        // Email y nombre de usuario (que es el email) son únicos POR
        // ORGANIZACIÓN, no en toda la tabla (RA-869f1xc0u): la misma persona
        // puede tener cuenta en varios centros. Identity crea "UserNameIndex"
        // único y "EmailIndex" no único sobre una sola columna; se sustituyen
        // por índices únicos (OrganizationId, columna) con el mismo nombre.
        ReplaceWithOrganizationUniqueIndex(builder, nameof(User.NormalizedUserName), "UserNameIndex");
        ReplaceWithOrganizationUniqueIndex(builder, nameof(User.NormalizedEmail), "EmailIndex");

        builder.HasIndex(u => u.OrganizationId);

        builder.HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    private static void ReplaceWithOrganizationUniqueIndex(
        EntityTypeBuilder<User> builder, string propertyName, string indexName)
    {
        // Un índice ya definido no admite cambiar sus columnas: se quita el de
        // Identity y se declara el nuevo.
        var identityIndex = builder.Metadata.FindIndex(builder.Metadata.FindProperty(propertyName)!);
        if (identityIndex is not null)
        {
            builder.Metadata.RemoveIndex(identityIndex);
        }

        builder.HasIndex(nameof(User.OrganizationId), propertyName)
            .IsUnique()
            .HasDatabaseName(indexName);
    }
}