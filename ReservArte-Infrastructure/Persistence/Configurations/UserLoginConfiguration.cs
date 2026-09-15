using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
{
    public void Configure(EntityTypeBuilder<UserLogin> builder)
    {
        // Tabla (AspNetUserLogins) y columnas las mapea IdentityUserContext.
        // Identity define la clave (LoginProvider, ProviderKey), única en toda la
        // tabla; se sustituye por una por organización (RA-869f1xc0u) para que
        // el mismo sujeto del proveedor pueda vincularse en varios centros.
        builder.HasKey(l => new { l.OrganizationId, l.LoginProvider, l.ProviderKey });
    }
}
