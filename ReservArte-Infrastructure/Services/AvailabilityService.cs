using Microsoft.Extensions.Logging;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Cálculo de la disponibilidad de la agenda (RA-869d7f4rd).
///
/// Todo el cálculo se hace **en minutos desde medianoche** en lugar de sumar
/// sobre <see cref="TimeOnly"/>: `TimeOnly.AddMinutes` da la vuelta al pasar de
/// las 23:59, así que un tramo mal construido generaría huecos al principio del
/// día en vez de terminar. Con enteros, pasarse del día es simplemente un
/// número mayor que 1440 y el bucle para.
///
/// No hay `IUnitOfWork` ni escritura alguna: es un servicio de solo lectura.
/// </summary>
public class AvailabilityService : IAvailabilityService
{
    /// <summary>
    /// Paso de la rejilla de huecos, en minutos (decisión del usuario): los
    /// huecos se ofrecen a las en punto, y cuarto, y media y menos cuarto de
    /// cada hora, que es como trabajan estos centros. Bajarlo da más opciones a
    /// la clienta y alarga la lista; subirlo desperdicia agenda con servicios de
    /// 20 o 45 minutos.
    /// </summary>
    public const int SlotStepMinutes = 15;

    /// <summary>
    /// Techo de la duración admitida. No es una regla de negocio, es un tope de
    /// cordura: sin él, una duración absurda haría recorrer la rejilla entera
    /// para no devolver ningún hueco.
    /// </summary>
    public const int MaxDurationMinutes = 12 * 60;

    private const int MinutesPerDay = 24 * 60;

    /// <summary>
    /// Zona horaria del negocio, para saber qué es «hoy» y qué hora es «ahora»
    /// al descartar los huecos ya pasados (decisión del usuario).
    ///
    /// Está fija aquí a propósito y es deuda conocida: el producto se
    /// redistribuye y cada centro debería traer la suya en `OrganizationSettings`
    /// (llega con RA-869f2gtyv). Mientras tanto, todos los centros son españoles.
    /// </summary>
    public const string BusinessTimeZoneId = "Europe/Madrid";

    private readonly IEmployeeRepository _employees;
    private readonly IAppointmentRepository _appointments;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(
        IEmployeeRepository employees,
        IAppointmentRepository appointments,
        ICurrentOrganizationService currentOrganization,
        TimeProvider timeProvider,
        ILogger<AvailabilityService> logger)
    {
        _employees = employees;
        _appointments = appointments;
        _currentOrganization = currentOrganization;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<AvailabilityResponse>> GetAvailableSlotsAsync(
        int employeeId,
        DateOnly date,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved<AvailabilityResponse>();
        }

        if (durationMinutes <= 0 || durationMinutes > MaxDurationMinutes)
        {
            return Result<AvailabilityResponse>.Fail(
                ErrorCodes.GenValidationFailed,
                "La petición no supera las validaciones.",
                new[]
                {
                    new ApiErrorDetail
                    {
                        Field = "durationMinutes",
                        Code = "INVALID_DURATION",
                        Message =
                            $"La duración debe estar entre 1 y {MaxDurationMinutes} minutos.",
                    },
                });
        }

        if (!await ActiveEmployeeExistsAsync(employeeId, cancellationToken))
        {
            return NotFound<AvailabilityResponse>(employeeId);
        }

        var empty = new AvailabilityResponse
        {
            EmployeeId = employeeId,
            Date = date,
            DurationMinutes = durationMinutes,
            SlotStepMinutes = SlotStepMinutes,
        };

        // El reloj del negocio se lee una sola vez: hace falta para saber si la
        // fecha ya pasó y, si es hoy, desde qué hora quedan huecos.
        var now = NowInBusinessTimeZone();
        var today = now is { } instant ? DateOnly.FromDateTime(instant.DateTime) : (DateOnly?)null;

        // Un día que ya pasó entero no tiene huecos que ofrecer. Se resuelve
        // antes de ir a la base de datos: son tres consultas que sobran.
        if (today is { } localToday && date < localToday)
        {
            return Result<AvailabilityResponse>.Ok(empty);
        }

        var schedule = await DayScheduleAsync(employeeId, date, cancellationToken);
        if (schedule.Count == 0)
        {
            return Result<AvailabilityResponse>.Ok(empty);
        }

        var busy = await BusyRangesAsync(employeeId, date, excludeAppointmentId: null, cancellationToken);

        // Hora a partir de la cual se ofrecen huecos: solo mordisquea el día de
        // hoy; ayer ya salió arriba y mañana empieza de cero. Sin zona horaria
        // resoluble, `today` es null y no se descarta nada.
        var notBefore = today == date && now is { } nowToday
            ? (nowToday.Hour * 60) + nowToday.Minute
            : 0;

        var slots = new List<TimeSlotDto>();

        foreach (var window in schedule)
        {
            var start = Math.Max(window.Start, notBefore);

            // La rejilla se ancla al inicio del tramo del horario, no a la hora
            // actual: así los huecos caen siempre en los mismos minutos del
            // reloj y no se desplazan según cuándo se consulte.
            if (start > window.Start)
            {
                var stepsSkipped = (start - window.Start + SlotStepMinutes - 1) / SlotStepMinutes;
                start = window.Start + (stepsSkipped * SlotStepMinutes);
            }

            for (var from = start; from + durationMinutes <= window.End; from += SlotStepMinutes)
            {
                var candidate = new MinuteRange(from, from + durationMinutes);

                if (busy.Any(b => b.Overlaps(candidate)))
                {
                    continue;
                }

                slots.Add(new TimeSlotDto
                {
                    StartTime = ToTimeOnly(candidate.Start),
                    EndTime = ToTimeOnly(candidate.End),
                });
            }
        }

        return Result<AvailabilityResponse>.Ok(new AvailabilityResponse
        {
            EmployeeId = employeeId,
            Date = date,
            DurationMinutes = durationMinutes,
            SlotStepMinutes = SlotStepMinutes,
            // Dos tramos del horario no deberían solaparse —el PUT de
            // disponibilidad lo valida—, pero ordenar y deduplicar aquí cuesta
            // nada y evita ofrecer dos veces el mismo hueco si alguno se coló
            // por SQL.
            Slots = slots
                .DistinctBy(s => s.StartTime)
                .OrderBy(s => s.StartTime)
                .ToList(),
        });
    }

    public async Task<Result<bool>> EnsureSlotAvailableAsync(
        int employeeId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved<bool>();
        }

        if (endTime <= startTime)
        {
            return Result<bool>.Fail(
                ErrorCodes.GenValidationFailed,
                "La petición no supera las validaciones.",
                new[]
                {
                    new ApiErrorDetail
                    {
                        Field = "endTime",
                        Code = "INVALID_RANGE",
                        Message = "La hora de fin debe ser posterior a la de inicio.",
                    },
                });
        }

        if (!await ActiveEmployeeExistsAsync(employeeId, cancellationToken))
        {
            return NotFound<bool>(employeeId);
        }

        var requested = new MinuteRange(ToMinutes(startTime), ToMinutes(endTime));

        var schedule = await DayScheduleAsync(employeeId, date, cancellationToken);
        if (!schedule.Any(w => w.Contains(requested)))
        {
            return SlotUnavailable(
                "El horario del empleado no cubre ese tramo en esa fecha.");
        }

        var exceptions = await DayExceptionsAsync(employeeId, date, cancellationToken);
        if (exceptions.Any(e => e.Overlaps(requested)))
        {
            return SlotUnavailable("El empleado tiene una ausencia en ese tramo.");
        }

        var appointments = await DayAppointmentsAsync(
            employeeId, date, excludeAppointmentId, cancellationToken);

        if (appointments.Any(a => a.Overlaps(requested)))
        {
            return SlotUnavailable("El empleado ya tiene una cita en ese tramo.");
        }

        return Result<bool>.Ok(true);
    }

    // ── Piezas del cálculo ────────────────────────────────────────────────

    /// <summary>
    /// Tramos del horario semanal que aplican a esa fecha, ya descontadas las
    /// ausencias. Lo que queda es el tiempo en el que el empleado trabaja.
    /// </summary>
    private async Task<IReadOnlyList<MinuteRange>> DayScheduleAsync(
        int employeeId, DateOnly date, CancellationToken cancellationToken)
    {
        var availabilities = await _employees.GetAvailabilitiesAsync(employeeId, cancellationToken);

        // OJO: el día del proyecto es lunes = 0, no el int de DayOfWeek
        // (domingo = 0). La conversión vive en WeekDay para no repetir el
        // desfase en cada consulta.
        var projectDay = WeekDay.FromDate(date);

        return availabilities
            .Where(a => a.DayOfWeek == projectDay)
            .Select(a => new MinuteRange(ToMinutes(a.StartTime), ToMinutes(a.EndTime)))
            .Where(r => r.End > r.Start)
            .OrderBy(r => r.Start)
            .ToList();
    }

    /// <summary>
    /// Ausencias del empleado recortadas a ese día. Una ausencia de varios días
    /// entra con el trozo que toca: fuera de este día no dice nada del horario.
    /// </summary>
    private async Task<IReadOnlyList<MinuteRange>> DayExceptionsAsync(
        int employeeId, DateOnly date, CancellationToken cancellationToken)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        var exceptions = await _employees.GetExceptionsAsync(
            employeeId, dayStart, dayEnd, cancellationToken);

        return exceptions
            .Select(e => new MinuteRange(
                ClampToDay(e.StartDateTime, dayStart, floor: true),
                ClampToDay(e.EndDateTime, dayStart, floor: false)))
            .Where(r => r.End > r.Start)
            .ToList();
    }

    /// <summary>
    /// Citas del empleado ese día que **ocupan agenda**. Las canceladas y las no
    /// presentadas liberan el hueco, así que no cuentan.
    /// </summary>
    private async Task<IReadOnlyList<MinuteRange>> DayAppointmentsAsync(
        int employeeId,
        DateOnly date,
        int? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        var appointments = await _appointments.GetByDateRangeAsync(
            date, date, employeeId, cancellationToken);

        return appointments
            .Where(a => a.Id != excludeAppointmentId)
            .Where(a => AppointmentStatuses.Blocking.Contains(a.Status))
            .Select(a => new MinuteRange(ToMinutes(a.StartTime), ToMinutes(a.EndTime)))
            .Where(r => r.End > r.Start)
            .ToList();
    }

    /// <summary>Todo lo que impide reservar ese día: ausencias y citas vivas.</summary>
    private async Task<IReadOnlyList<MinuteRange>> BusyRangesAsync(
        int employeeId,
        DateOnly date,
        int? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        var exceptions = await DayExceptionsAsync(employeeId, date, cancellationToken);
        var appointments = await DayAppointmentsAsync(
            employeeId, date, excludeAppointmentId, cancellationToken);

        return exceptions.Concat(appointments).ToList();
    }

    private async Task<bool> ActiveEmployeeExistsAsync(
        int employeeId, CancellationToken cancellationToken)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        return employee is { IsActive: true };
    }

    // ── Reloj ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Instante actual en la zona del negocio, o null si esa zona no se pudo
    /// resolver en esta máquina. Null significa «no filtres por hora»: es
    /// preferible ofrecer un hueco ya pasado, que la clienta verá rechazado al
    /// reservar, que esconder la agenda entera de un día bueno.
    /// </summary>
    private DateTimeOffset? NowInBusinessTimeZone()
    {
        var timeZone = BusinessTimeZone();

        return timeZone is null
            ? null
            : TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), timeZone);
    }

    /// <summary>
    /// Resuelve la zona del negocio. .NET 8 acepta identificadores IANA también
    /// en Windows, pero si la máquina no trae la base de datos de zonas (una
    /// imagen recortada, por ejemplo) no se puede: se registra y se sigue sin
    /// filtrar por hora, que es lo menos malo.
    /// </summary>
    private TimeZoneInfo? BusinessTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(BusinessTimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            _logger.LogWarning(
                ex,
                "No se pudo resolver la zona horaria {TimeZoneId}; los huecos ya pasados no se descartarán",
                BusinessTimeZoneId);

            return null;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static int ToMinutes(TimeOnly time) => (time.Hour * 60) + time.Minute;

    private static TimeOnly ToTimeOnly(int minutes) =>
        new(minutes / 60, minutes % 60);

    /// <summary>
    /// Pasa un instante a minutos dentro del día indicado, recortando lo que se
    /// salga por cualquiera de los dos extremos.
    /// </summary>
    private static int ClampToDay(DateTime instant, DateTime dayStart, bool floor)
    {
        var minutes = (int)Math.Round((instant - dayStart).TotalMinutes, MidpointRounding.ToZero);

        return floor
            ? Math.Max(minutes, 0)
            : Math.Min(minutes, MinutesPerDay);
    }

    private static Result<T> TenantNotResolved<T>() =>
        Result<T>.Fail(
            ErrorCodes.OrgTenantNotResolved,
            "No se ha podido resolver la organización de la petición.");

    /// <summary>
    /// Mismo resultado para «no existe», «es de otra organización» y «está de
    /// baja»: quien consulta huecos no tiene por qué distinguirlos, y separarlos
    /// revelaría qué ids existen en otros centros.
    /// </summary>
    private static Result<T> NotFound<T>(int employeeId) =>
        Result<T>.Fail(
            ErrorCodes.GenNotFound,
            $"No existe un empleado activo con id {employeeId}.");

    private static Result<bool> SlotUnavailable(string message) =>
        Result<bool>.Fail(ErrorCodes.AptSlotUnavailable, message);

    /// <summary>
    /// Intervalo semiabierto <c>[Start, End)</c> en minutos desde medianoche.
    /// Semiabierto para que dos citas contiguas (11:00-12:00 y 12:00-13:00) no
    /// se consideren solapadas.
    /// </summary>
    private readonly record struct MinuteRange(int Start, int End)
    {
        public bool Overlaps(MinuteRange other) => Start < other.End && other.Start < End;

        public bool Contains(MinuteRange other) => other.Start >= Start && other.End <= End;
    }
}
