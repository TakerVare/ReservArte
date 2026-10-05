using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Disponibilidad por servicio para la pantalla de reserva (RA-869fagpx9, H-44 y
/// H-45). Mismo cálculo que <see cref="AvailabilityService"/> (<see cref="SlotGrid"/>),
/// pero cargando los datos de cada empleado una sola vez para todo el intervalo:
/// horario, ausencias y citas son tres consultas por empleado, no tres por día.
/// </summary>
public class ServiceAvailabilityService : IServiceAvailabilityService
{
    /// <summary>Tope del intervalo de días: un mes de calendario con margen.</summary>
    public const int MaxRangeDays = 62;

    private readonly IServiceRepository _services;
    private readonly IEmployeeRepository _employees;
    private readonly IAppointmentRepository _appointments;
    private readonly IOrganizationRepository _organizations;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly ICurrentUserService _currentUser;
    private readonly IBusinessClock _businessClock;
    private readonly TimeProvider _timeProvider;

    public ServiceAvailabilityService(
        IServiceRepository services,
        IEmployeeRepository employees,
        IAppointmentRepository appointments,
        IOrganizationRepository organizations,
        ICurrentOrganizationService currentOrganization,
        ICurrentUserService currentUser,
        IBusinessClock businessClock,
        TimeProvider timeProvider)
    {
        _services = services;
        _employees = employees;
        _appointments = appointments;
        _organizations = organizations;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _businessClock = businessClock;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ServiceAvailabilityResponse>> GetSlotsAsync(
        int serviceId, DateOnly date, CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved<ServiceAvailabilityResponse>();
        }

        var service = await _services.GetByIdAsync(serviceId, cancellationToken);
        if (service is null || !service.IsActive)
        {
            return NotFound<ServiceAvailabilityResponse>(serviceId);
        }

        var clock = await ClockAsync(cancellationToken);
        var employees = new List<EmployeeSlotsDto>();

        if (date >= clock.Window.BookableFrom && date <= clock.Window.BookableUntil)
        {
            foreach (var employee in await _employees.GetActiveForServiceAsync(serviceId, cancellationToken))
            {
                var agenda = await AgendaAsync(employee.Id, date, date, clock.TimeZone, cancellationToken);
                var slots = agenda.SlotsFor(date, service.DurationMinutes, clock);
                if (slots.Count > 0)
                {
                    employees.Add(new EmployeeSlotsDto
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employee.FirstName,
                        Slots = slots,
                    });
                }
            }
        }

        return Result<ServiceAvailabilityResponse>.Ok(new ServiceAvailabilityResponse
        {
            ServiceId = serviceId,
            Date = date,
            DurationMinutes = service.DurationMinutes,
            SlotStepMinutes = AvailabilityService.SlotStepMinutes,
            BookableFrom = clock.Window.BookableFrom,
            BookableUntil = clock.Window.BookableUntil,
            Employees = employees,
        });
    }

    public async Task<Result<ServiceAvailableDaysResponse>> GetAvailableDaysAsync(
        int serviceId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved<ServiceAvailableDaysResponse>();
        }

        if (to < from)
        {
            return FieldError<ServiceAvailableDaysResponse>("to", "La fecha final no puede ser anterior a la inicial.");
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
        {
            return FieldError<ServiceAvailableDaysResponse>("to", $"El intervalo no puede pasar de {MaxRangeDays} días.");
        }

        var service = await _services.GetByIdAsync(serviceId, cancellationToken);
        if (service is null || !service.IsActive)
        {
            return NotFound<ServiceAvailableDaysResponse>(serviceId);
        }

        var clock = await ClockAsync(cancellationToken);
        var start = from > clock.Window.BookableFrom ? from : clock.Window.BookableFrom;
        var end = to < clock.Window.BookableUntil ? to : clock.Window.BookableUntil;
        var days = new SortedSet<DateOnly>();

        if (start <= end)
        {
            foreach (var employee in await _employees.GetActiveForServiceAsync(serviceId, cancellationToken))
            {
                var agenda = await AgendaAsync(employee.Id, start, end, clock.TimeZone, cancellationToken);
                for (var day = start; day <= end; day = day.AddDays(1))
                {
                    if (!days.Contains(day) && agenda.SlotsFor(day, service.DurationMinutes, clock).Count > 0)
                    {
                        days.Add(day);
                    }
                }
            }
        }

        return Result<ServiceAvailableDaysResponse>.Ok(new ServiceAvailableDaysResponse
        {
            ServiceId = serviceId,
            From = from,
            To = to,
            BookableFrom = clock.Window.BookableFrom,
            BookableUntil = clock.Window.BookableUntil,
            Days = days.ToList(),
        });
    }

    // ── Reloj y ventana ───────────────────────────────────────────────────

    private sealed record Clock(DateTime Now, TimeZoneInfo TimeZone, BookingWindowDto Window)
    {
        public DateOnly Today => DateOnly.FromDateTime(Now);
    }

    private async Task<Clock> ClockAsync(CancellationToken cancellationToken)
    {
        var timeZone = await _businessClock.FindTimeZoneAsync(cancellationToken) ?? TimeZoneInfo.Utc;
        var now = BusinessClock.Now(_timeProvider, timeZone);
        var today = DateOnly.FromDateTime(now);
        var organization = await _organizations.GetCurrentAsync(cancellationToken);

        // La ventana es la del rol de quien consulta: el personal ve más semanas.
        var weeks = IsStaff
            ? organization?.StaffBookingWindowWeeks ?? 10
            : organization?.CustomerBookingWindowWeeks ?? 6;

        return new Clock(now, timeZone, new BookingWindowDto
        {
            BookableFrom = today,
            BookableUntil = today.AddDays(weeks * 7),
        });
    }

    private bool IsStaff => _currentUser.Role is Roles.Admin or Roles.Manager or Roles.Employee;

    // ── Agenda de un empleado en un intervalo ─────────────────────────────

    /// <summary>Horario, ausencias y citas que ocupan agenda de un empleado en el intervalo.</summary>
    private sealed class Agenda(
        IReadOnlyList<EmployeeAvailability> schedule,
        IReadOnlyList<EmployeeException> exceptions,
        IReadOnlyList<Appointment> appointments,
        TimeZoneInfo timeZone)
    {
        public List<TimeSlotDto> SlotsFor(DateOnly day, int durationMinutes, Clock clock)
        {
            if (day < clock.Today)
            {
                return [];
            }

            var projectDay = WeekDay.FromDate(day);
            var dayWindows = schedule
                .Where(a => a.DayOfWeek == projectDay)
                .Select(a => new MinuteRange(SlotGrid.ToMinutes(a.StartTime), SlotGrid.ToMinutes(a.EndTime)))
                .Where(r => r.End > r.Start)
                .OrderBy(r => r.Start)
                .ToList();
            if (dayWindows.Count == 0)
            {
                return [];
            }

            var dayStart = day.ToDateTime(TimeOnly.MinValue);
            var busy = exceptions
                .Select(e => new MinuteRange(
                    SlotGrid.ClampToDay(SlotGrid.ToLocal(e.StartDateTime, timeZone), dayStart, floor: true),
                    SlotGrid.ClampToDay(SlotGrid.ToLocal(e.EndDateTime, timeZone), dayStart, floor: false)))
                .Concat(appointments
                    .Where(a => a.AppointmentDate == day)
                    .Select(a => new MinuteRange(SlotGrid.ToMinutes(a.StartTime), SlotGrid.ToMinutes(a.EndTime))))
                .Where(r => r.End > r.Start)
                .ToList();

            var notBefore = day == clock.Today ? (clock.Now.Hour * 60) + clock.Now.Minute : 0;

            return SlotGrid.Compute(dayWindows, busy, notBefore, durationMinutes, AvailabilityService.SlotStepMinutes);
        }
    }

    private async Task<Agenda> AgendaAsync(
        int employeeId, DateOnly from, DateOnly to, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        var schedule = await _employees.GetAvailabilitiesAsync(employeeId, cancellationToken);

        var exceptions = await _employees.GetExceptionsAsync(
            employeeId,
            TimeZoneInfo.ConvertTimeToUtc(from.ToDateTime(TimeOnly.MinValue), timeZone),
            TimeZoneInfo.ConvertTimeToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone),
            cancellationToken);

        // Las canceladas y las no presentadas liberan el hueco.
        var appointments = (await _appointments.GetByDateRangeAsync(from, to, employeeId, cancellationToken))
            .Where(a => AppointmentStatuses.Blocking.Contains(a.Status))
            .ToList();

        return new Agenda(schedule, exceptions, appointments, timeZone);
    }

    // ── Resultados ────────────────────────────────────────────────────────

    private static Result<T> TenantNotResolved<T>() =>
        Result<T>.Fail(ErrorCodes.OrgTenantNotResolved, "No se ha podido resolver la organización de la petición.");

    private static Result<T> NotFound<T>(int serviceId) =>
        Result<T>.Fail(ErrorCodes.GenNotFound, $"No existe un servicio activo con id {serviceId}.");

    private static Result<T> FieldError<T>(string field, string message) =>
        Result<T>.Fail(
            ErrorCodes.GenValidationFailed,
            "La petición no supera las validaciones.",
            new[] { new ApiErrorDetail { Field = field, Code = "INVALID_RANGE", Message = message } });
}
