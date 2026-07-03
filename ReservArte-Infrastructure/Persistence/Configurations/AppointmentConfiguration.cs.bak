using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("pending");
        builder.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)");
        builder.Property(e => e.DepositAmount).HasColumnType("decimal(10,2)");
        builder.Property(e => e.RedsysOrderNumber).HasMaxLength(50);
        builder.Property(e => e.RedsysPreAuthToken).HasMaxLength(200);
        builder.Property(e => e.CancellationReason).HasMaxLength(500);
        builder.Property(e => e.CancelledByType).HasMaxLength(50);

        builder.HasOne(e => e.Customer)
               .WithMany(c => c.Appointments)
               .HasForeignKey(e => e.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Employee)
               .WithMany(emp => emp.Appointments)
               .HasForeignKey(e => e.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PaymentMethod)
               .WithMany(pm => pm.Appointments)
               .HasForeignKey(e => e.PaymentMethodId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.RedsysOrderNumber).IsUnique()
               .HasFilter("[RedsysOrderNumber] IS NOT NULL");
        builder.HasIndex(e => new { e.AppointmentDate, e.EmployeeId });
    }
}
