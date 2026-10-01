using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Disponibilidad por servicio para la pantalla de reserva (RA-869fagpx9, H-44 y
/// H-45): qué empleados pueden prestar un servicio y cuándo. Complementa a
/// <see cref="IAvailabilityService"/>, que responde por un empleado concreto, con el
/// mismo cálculo de huecos.
///
/// La ventana de reserva depende de quién consulta: la clienta ve sus semanas
/// (<c>CustomerBookingWindowWeeks</c>) y el personal las suyas
/// (<c>StaffBookingWindowWeeks</c>), siempre desde hoy.
/// </summary>
public interface IServiceAvailabilityService
{
    /// <summary>
    /// Huecos del servicio en esa fecha, agrupados por empleado. Servicio inexistente
    /// o de baja → GEN_NOT_FOUND. Fecha fuera de la ventana → lista vacía.
    /// </summary>
    Task<Result<ServiceAvailabilityResponse>> GetSlotsAsync(
        int serviceId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Días del intervalo [<paramref name="from"/>, <paramref name="to"/>] con al menos
    /// un hueco, recortado a la ventana de reserva. Intervalo de más de 62 días o al
    /// revés → GEN_VALIDATION_FAILED.
    /// </summary>
    Task<Result<ServiceAvailableDaysResponse>> GetAvailableDaysAsync(
        int serviceId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
