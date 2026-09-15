using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class CustomerNoteConfiguration : IEntityTypeConfiguration<CustomerNote>
{
    public void Configure(EntityTypeBuilder<CustomerNote> builder)
    {
        builder.ToTable("CustomerNotes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Note).HasMaxLength(2000).IsRequired();

        // Las notas se leen por cliente, de la más reciente a la más antigua.
        builder.HasIndex(n => new { n.CustomerId, n.CreatedAt });
        builder.HasIndex(n => n.OrganizationId);

        builder.HasOne(n => n.Customer)
               .WithMany(c => c.Notes)
               .HasForeignKey(n => n.CustomerId)
               .OnDelete(DeleteBehavior.Cascade);

        // Autor. Restrict y no Cascade: la nota es histórico del cliente y no
        // debe desaparecer con la ficha de quien la escribió. Además, SQL Server
        // rechaza dos caminos en cascada desde AspNetUsers (vía Customer y vía
        // Employee) hasta esta tabla.
        builder.HasOne(n => n.Employee)
               .WithMany()
               .HasForeignKey(n => n.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Organization)
               .WithMany()
               .HasForeignKey(n => n.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
