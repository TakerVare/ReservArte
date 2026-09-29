using ReservArte.Domain.Entities;

namespace ReservArte.IntegrationTests.Infrastructure;

/// <summary>
/// Escenario de citas de un centro: una empleada nueva con horario de 09:00 a 18:00
/// todos los días, una clienta y dos servicios asignados a la empleada (cejas, 45 min
/// y 25 €, con la variación «Con hilo», +15 min y +5 €; y tinte, 30 min y 20 €).
/// </summary>
public sealed record AppointmentScene(
    Guid OrganizationId, Employee Employee, Customer Customer, Service Brows, int BrowsThread, Service Tint)
{
    /// <summary>Lunes lejano en el futuro, para que nada dependa del reloj.</summary>
    public static readonly DateOnly Day = new(2031, 3, 3);

    /// <summary>Cuerpo de alta para esta clienta y esta empleada el día <see cref="Day"/>.</summary>
    public AppointmentBody Booking(TimeOnly start, params (int ServiceId, int? VariationId)[] items) =>
        new(Customer.Id, Employee.Id, Day, start,
            items.Select(i => (object)new { serviceId = i.ServiceId, serviceVariationId = i.VariationId }).ToArray(),
            null);

    /// <summary>Alta con solo el tinte (30 min).</summary>
    public AppointmentBody Tinting(TimeOnly start) => Booking(start, (Tint.Id, null));
}

/// <summary>Cuerpo de <c>POST /api/v1/appointments</c>.</summary>
public sealed record AppointmentBody(
    int CustomerId, int EmployeeId, DateOnly AppointmentDate, TimeOnly StartTime, object[] Items, string? Notes);
