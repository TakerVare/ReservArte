using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", t =>
        {
            t.HasCheckConstraint(
                "CK_Appointments_Status",
                CatalogCheck.In(nameof(Appointment.Status), AppointmentStatuses.All));

            // La columna es nullable y un CHECK solo rechaza lo que evalúa a
            // FALSE, así que «sin cancelar» pasa. Status sigue siendo la fuente
            // de verdad; la coherencia entre ambos la impone RA-869d7f4xf.
            t.HasCheckConstraint(
                "CK_Appointments_CancelledByType",
                CatalogCheck.In(nameof(Appointment.CancelledByType), AppointmentCancelledByTypes.All));

            // Un importe negativo restaría del total facturado.
            t.HasCheckConstraint(
                "CK_Appointments_Amounts",
                "[TotalPrice] >= 0 AND [DepositAmount] >= 0");

            // EndTime lo calcula el servicio sumando las líneas: una cita que
            // acabe antes de empezar no ocuparía hueco en la agenda y rompería
            // la detección de solapes. Mismo criterio que el horario semanal.
            t.HasCheckConstraint("CK_Appointments_EndTime", "[EndTime] > [StartTime]");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AppointmentDate).HasColumnType("date");
        builder.Property(a => a.StartTime).HasColumnType("time");
        builder.Property(a => a.EndTime).HasColumnType("time");

        builder.Property(a => a.Status).HasMaxLength(50).IsRequired();
        builder.Property(a => a.CancelledByType).HasMaxLength(20);

        builder.Property(a => a.TotalPrice).HasColumnType("decimal(10,2)");
        builder.Property(a => a.DepositAmount).HasColumnType("decimal(10,2)");

        // Longitudes del sketch de diseño (vol. 1 §5.2).
        builder.Property(a => a.RedsysOrderNumber).HasMaxLength(20);
        builder.Property(a => a.RedsysPreAuthToken).HasMaxLength(255);

        // El sketch los deja en NVARCHAR(MAX); se acotan como el resto del
        // esquema (precedente: CustomerNote.Note, 2000). Los validadores de
        // RA-869d7f519 deben respetar estos límites.
        builder.Property(a => a.CancellationReason).HasMaxLength(500);
        builder.Property(a => a.Notes).HasMaxLength(2000);

        // La agenda se consulta siempre por organización y día (vol. 1 §5.2).
        builder.HasIndex(a => new { a.OrganizationId, a.AppointmentDate })
               .HasDatabaseName("idx_appointments_org_date");

        // Único, pero FILTRADO: en SQL Server un índice único admite un solo
        // NULL, y la mayoría de las citas no pasan por Redsys. Sin el filtro, la
        // segunda cita sin número de pedido chocaría con la primera.
        builder.HasIndex(a => a.RedsysOrderNumber)
               .IsUnique()
               .HasFilter("[RedsysOrderNumber] IS NOT NULL")
               .HasDatabaseName("idx_appointments_redsys_order");

        // La agenda de una empleada y el historial de una clienta (RA-869f2gn91).
        builder.HasIndex(a => new { a.EmployeeId, a.AppointmentDate });
        builder.HasIndex(a => a.CustomerId);

        // Restrict en las dos, por dos motivos. Una cita es histórico de negocio
        // (importes cobrados, señal de Redsys) y no puede irse por delante con
        // una ficha; y SQL Server rechaza los DOS caminos en cascada que llegan
        // aquí desde AspNetUsers, vía Customers y vía Employees. Mismo
        // precedente que CustomerNotes.EmployeeId. Las bajas son lógicas, así
        // que en el producto nadie borra fichas.
        builder.HasOne(a => a.Customer)
               .WithMany()
               .HasForeignKey(a => a.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Employee)
               .WithMany()
               .HasForeignKey(a => a.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Organization)
               .WithMany()
               .HasForeignKey(a => a.OrganizationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
