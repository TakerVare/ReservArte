using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
    public void Configure(EntityTypeBuilder<MessageTemplate> builder)
    {
        builder.ToTable("MessageTemplates", t =>
            t.HasCheckConstraint(
                "CK_MessageTemplates_Type",
                CatalogCheck.In(nameof(MessageTemplate.Type), MessageTemplateTypes.All)));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Type).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Subject).HasMaxLength(200);

        // Acotado como el resto de textos largos del esquema: un email con su
        // HTML cabe de sobra, y el validador de la API tendrá un tope que citar.
        builder.Property(m => m.Body).HasMaxLength(10000).IsRequired();
        builder.Property(m => m.Language).HasMaxLength(5).HasDefaultValue("es").IsRequired();

        // El nombre identifica la plantilla al elegirla: único entre las vigentes
        // del centro. Una retirada no bloquea que se vuelva a usar su nombre.
        builder.HasIndex(m => new { m.OrganizationId, m.Name })
               .IsUnique()
               .HasFilter("\"IsActive\" = TRUE");

        builder.HasOne(m => m.Organization)
               .WithMany()
               .HasForeignKey(m => m.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
