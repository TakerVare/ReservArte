using Microsoft.Extensions.Logging;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.Interfaces;
using ReservArte.Application.Mapping;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Agenda y reserva de citas (RA-869d7f519). Las reglas de quién puede qué están
/// en <see cref="IAppointmentBookingService"/>; las transiciones de estado, en
/// <see cref="AppointmentService"/>.
///
/// Alta y edición siguen el mismo guion: comprobar quién llama, resolver la
/// empleada y las líneas contra el catálogo (existen, están activas y la empleada
/// presta esos servicios), calcular fin, precio y duración, comprobar el hueco con
/// <see cref="IAvailabilityService"/> y guardar.
///
/// Carrera conocida: dos altas simultáneas sobre el mismo hueco pueden pasar las
/// dos la comprobación. Con un centro y el personal reservando es improbable; el
/// cierre definitivo es una restricción de exclusión en PostgreSQL (vol. 1 §5.2).
/// </summary>
public class AppointmentBookingService : IAppointmentBookingService
{
    private const int MinutesPerDay = 24 * 60;

    private readonly IAppointmentRepository _appointments;
    private readonly ICustomerRepository _customers;
    private readonly IEmployeeRepository _employees;
    private readonly IServiceRepository _services;
    private readonly IAvailabilityService _availability;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AppointmentBookingService> _logger;

    public AppointmentBookingService(
        IAppointmentRepository appointments,
        ICustomerRepository customers,
        IEmployeeRepository employees,
        IServiceRepository services,
        IAvailabilityService availability,
        ICurrentOrganizationService currentOrganization,
        ICurrentUserService currentUser,
        ILogger<AppointmentBookingService> logger)
    {
        _appointments = appointments;
        _customers = customers;
        _employees = employees;
        _services = services;
        _availability = availability;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _logger = logger;
    }

    // ── Lectura ───────────────────────────────────────────────────────────

    public async Task<Result<PagedResult<AppointmentSummaryDto>>> GetPagedAsync(
        AppointmentFilter filter, CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved<PagedResult<AppointmentSummaryDto>>();
        }

        if (IsCustomer)
        {
            // La clienta solo ve sus citas: el filtro se impone, no se confía en el
            // que venga en la petición.
            filter = new AppointmentFilter
            {
                From = filter.From,
                To = filter.To,
                EmployeeId = filter.EmployeeId,
                CustomerId = _currentUser.UserId,
                Status = filter.Status,
                IsActive = filter.IsActive,
                Page = filter.Page,
                PageSize = filter.PageSize,
            };
        }
        else if (!IsStaff)
        {
            return Forbidden<PagedResult<AppointmentSummaryDto>>();
        }

        var page = await _appointments.GetPagedAsync(filter, cancellationToken);

        return Result<PagedResult<AppointmentSummaryDto>>.Ok(new PagedResult<AppointmentSummaryDto>
        {
            Items = page.Items.Select(AppointmentMapper.ToSummaryDto).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize,
        });
    }

    public async Task<Result<AppointmentDetailDto>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved<AppointmentDetailDto>();
        }

        if (!IsStaff && !IsCustomer)
        {
            return Forbidden<AppointmentDetailDto>();
        }

        var appointment = await _appointments.GetDetailAsync(id, cancellationToken);

        // Una clienta sobre una cita ajena recibe el mismo 404 que si no existiera.
        if (appointment is null || (IsCustomer && appointment.CustomerId != _currentUser.UserId))
        {
            return NotFound<AppointmentDetailDto>(id);
        }

        return Result<AppointmentDetailDto>.Ok(AppointmentMapper.ToDetailDto(appointment));
    }

    public async Task<Result<PagedResult<AppointmentDetailDto>>> GetCustomerHistoryAsync(
        int customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved<PagedResult<AppointmentDetailDto>>();
        }

        if (!IsStaff)
        {
            return Forbidden<PagedResult<AppointmentDetailDto>>();
        }

        // 404 y no lista vacía: un historial vacío dice «sin citas», no «no existe».
        if (await _customers.GetByIdAsync(customerId, cancellationToken) is null)
        {
            return Result<PagedResult<AppointmentDetailDto>>.Fail(
                ErrorCodes.GenNotFound, $"No existe la clienta con id {customerId}.");
        }

        var history = await _appointments.GetCustomerHistoryAsync(customerId, page, pageSize, cancellationToken);

        return Result<PagedResult<AppointmentDetailDto>>.Ok(new PagedResult<AppointmentDetailDto>
        {
            Items = history.Items.Select(AppointmentMapper.ToDetailDto).ToList(),
            TotalCount = history.TotalCount,
            Page = history.Page,
            PageSize = history.PageSize,
        });
    }

    // ── Alta y edición ────────────────────────────────────────────────────

    public async Task<Result<AppointmentDetailDto>> CreateAsync(
        CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is not { } organizationId)
        {
            return TenantNotResolved<AppointmentDetailDto>();
        }

        // El rol se comprueba antes de leer nada, como en las transiciones.
        if (!IsStaff)
        {
            return Forbidden<AppointmentDetailDto>();
        }

        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            return FieldError<AppointmentDetailDto>(
                "customerId", "La clienta indicada no existe o está dada de baja en este centro.");
        }

        if (customer.IsBlocked)
        {
            return Result<AppointmentDetailDto>.Fail(
                ErrorCodes.CustBlocked,
                "La clienta está bloqueada y no puede reservar citas.");
        }

        var plan = await PlanAsync(
            request.EmployeeId, request.AppointmentDate, request.StartTime, request.Items,
            excludeAppointmentId: null, cancellationToken);
        if (plan.Failure is not null)
        {
            return plan.Failure;
        }

        var appointment = new Appointment
        {
            OrganizationId = organizationId,
            CustomerId = customer.Id,
            EmployeeId = request.EmployeeId,
            AppointmentDate = request.AppointmentDate,
            StartTime = request.StartTime,
            EndTime = plan.EndTime,
            Status = AppointmentStatuses.Pending,
            TotalPrice = plan.TotalPrice,
            Notes = CleanNotes(request.Notes),
            CreatedById = _currentUser.UserId,
        };
        foreach (var item in plan.Items)
        {
            item.OrganizationId = organizationId;
            appointment.ServiceItems.Add(item);
        }

        _appointments.Add(appointment);
        await _appointments.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cita {AppointmentId} creada para la clienta {CustomerId} por la cuenta {UserId}",
            appointment.Id, appointment.CustomerId, _currentUser.UserId);

        return await DetailAsync(appointment.Id, cancellationToken);
    }

    public async Task<Result<AppointmentDetailDto>> UpdateAsync(
        int id, UpdateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is not { } organizationId)
        {
            return TenantNotResolved<AppointmentDetailDto>();
        }

        if (!IsStaff)
        {
            return Forbidden<AppointmentDetailDto>();
        }

        var appointment = await _appointments.GetForUpdateAsync(id, cancellationToken);
        if (appointment is null || !appointment.IsActive)
        {
            return NotFound<AppointmentDetailDto>(id);
        }

        // Solo se reorganiza lo que no ha empezado: una cita en curso o cerrada es
        // histórico, y cambiarle la hora o los servicios lo falsearía.
        if (appointment.Status is not (AppointmentStatuses.Pending or AppointmentStatuses.Confirmed))
        {
            return Result<AppointmentDetailDto>.Fail(
                ErrorCodes.AptInvalidState,
                $"Una cita en estado «{appointment.Status}» no se puede editar.");
        }

        var plan = await PlanAsync(
            request.EmployeeId, request.AppointmentDate, request.StartTime, request.Items,
            excludeAppointmentId: appointment.Id, cancellationToken);
        if (plan.Failure is not null)
        {
            return plan.Failure;
        }

        appointment.EmployeeId = request.EmployeeId;
        appointment.AppointmentDate = request.AppointmentDate;
        appointment.StartTime = request.StartTime;
        appointment.EndTime = plan.EndTime;
        appointment.TotalPrice = plan.TotalPrice;
        appointment.Notes = CleanNotes(request.Notes);

        // Las líneas se sustituyen enteras (precedente de los paquetes): quitar una
        // de la colección la borra, porque su FK a la cita es obligatoria.
        appointment.ServiceItems.Clear();
        foreach (var item in plan.Items)
        {
            item.OrganizationId = organizationId;
            appointment.ServiceItems.Add(item);
        }

        _appointments.Update(appointment);
        await _appointments.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cita {AppointmentId} editada por la cuenta {UserId}", appointment.Id, _currentUser.UserId);

        return await DetailAsync(appointment.Id, cancellationToken);
    }

    public async Task<Result<AppointmentDto>> DeactivateAsync(
        int id, CancellationToken cancellationToken = default)
    {
        if (!_currentOrganization.IsResolved)
        {
            return TenantNotResolved<AppointmentDto>();
        }

        if (!IsManagement)
        {
            return Forbidden<AppointmentDto>();
        }

        var appointment = await _appointments.GetByIdAsync(id, cancellationToken);
        if (appointment is null)
        {
            return NotFound<AppointmentDto>(id);
        }

        if (appointment.IsActive)
        {
            appointment.IsActive = false;
            _appointments.Update(appointment);
            await _appointments.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cita {AppointmentId} retirada por la cuenta {UserId}", appointment.Id, _currentUser.UserId);
        }

        return Result<AppointmentDto>.Ok(AppointmentMapper.ToDto(appointment));
    }

    // ── Cálculo común de alta y edición ───────────────────────────────────

    private sealed record Plan(
        TimeOnly EndTime,
        decimal TotalPrice,
        List<AppointmentServiceItem> Items,
        Result<AppointmentDetailDto>? Failure);

    private static Plan Failed(Result<AppointmentDetailDto> failure) =>
        new(default, 0, [], failure);

    /// <summary>
    /// Comprueba la empleada y las líneas contra el catálogo, calcula fin, precio y
    /// duración y comprueba que el hueco esté libre. Devuelve las líneas listas
    /// para guardar, o el fallo que corresponda.
    /// </summary>
    private async Task<Plan> PlanAsync(
        int employeeId,
        DateOnly date,
        TimeOnly start,
        IReadOnlyList<AppointmentItemRequest> requested,
        int? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null || !employee.IsActive)
        {
            return Failed(FieldError<AppointmentDetailDto>(
                "employeeId", "La empleada indicada no existe o está dada de baja en este centro."));
        }

        var assigned = await _employees.GetAssignedServiceIdsAsync(
            employeeId, requested.Select(i => i.ServiceId).ToList(), cancellationToken);

        var items = new List<AppointmentServiceItem>();
        var totalMinutes = 0;
        var totalPrice = 0m;

        for (var index = 0; index < requested.Count; index++)
        {
            var line = requested[index];
            var service = await _services.GetDetailAsync(line.ServiceId, cancellationToken);
            if (service is null || !service.IsActive)
            {
                return Failed(FieldError<AppointmentDetailDto>(
                    $"items[{index}].serviceId", "El servicio no existe o está dado de baja en este centro."));
            }

            if (!assigned.Contains(service.Id))
            {
                return Failed(FieldError<AppointmentDetailDto>(
                    $"items[{index}].serviceId", "La empleada no presta este servicio.", "EmployeeNotQualified"));
            }

            // GetDetailAsync trae solo las variaciones activas del servicio.
            ServiceVariation? variation = null;
            if (line.ServiceVariationId is { } variationId)
            {
                variation = service.Variations.FirstOrDefault(v => v.Id == variationId);
                if (variation is null)
                {
                    return Failed(FieldError<AppointmentDetailDto>(
                        $"items[{index}].serviceVariationId",
                        "La variación no existe, no es de este servicio o está dada de baja."));
                }
            }

            var minutes = service.DurationMinutes + (variation?.DurationModifier ?? 0);
            var price = service.BasePrice + (variation?.PriceModifier ?? 0m);

            totalMinutes += minutes;
            totalPrice += price;
            items.Add(new AppointmentServiceItem
            {
                ServiceId = service.Id,
                ServiceVariationId = variation?.Id,
                DurationMinutes = minutes,
                Price = price,
                Order = index + 1,
            });
        }

        // En minutos desde medianoche: TimeOnly.AddMinutes da la vuelta al pasar
        // de las 23:59 y una cita que acaba «a las 00:30» parecería más corta.
        // Acabar justo a medianoche tampoco cabe: TimeOnly no representa las 24:00.
        var endMinutes = start.Hour * 60 + start.Minute + totalMinutes;
        if (endMinutes >= MinutesPerDay)
        {
            return Failed(FieldError<AppointmentDetailDto>(
                "startTime", "La cita no cabe en el día: acabaría a medianoche o después."));
        }

        var end = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(endMinutes));

        var slot = await _availability.EnsureSlotAvailableAsync(
            employeeId, date, start, end, excludeAppointmentId, cancellationToken);
        if (!slot.Success)
        {
            return Failed(Result<AppointmentDetailDto>.Fail(slot.ErrorCode!, slot.ErrorMessage!, slot.ErrorDetails));
        }

        return new Plan(end, totalPrice, items, null);
    }

    private async Task<Result<AppointmentDetailDto>> DetailAsync(int id, CancellationToken cancellationToken)
    {
        var saved = await _appointments.GetDetailAsync(id, cancellationToken);
        return saved is null
            ? NotFound<AppointmentDetailDto>(id)
            : Result<AppointmentDetailDto>.Ok(AppointmentMapper.ToDetailDto(saved));
    }

    private static string? CleanNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    // ── Roles ─────────────────────────────────────────────────────────────

    private bool IsStaff => _currentUser.Role is Roles.Admin or Roles.Manager or Roles.Employee;

    private bool IsManagement => _currentUser.Role is Roles.Admin or Roles.Manager;

    private bool IsCustomer => _currentUser.Role == Roles.Customer;

    // ── Resultados ────────────────────────────────────────────────────────

    private static Result<T> TenantNotResolved<T>() =>
        Result<T>.Fail(
            ErrorCodes.OrgTenantNotResolved,
            "No se ha podido resolver la organización de la petición.");

    /// <summary>Mismo resultado para «no existe» y «es de otra organización».</summary>
    private static Result<T> NotFound<T>(int id) =>
        Result<T>.Fail(ErrorCodes.GenNotFound, $"No existe la cita con id {id}.");

    private static Result<T> Forbidden<T>() =>
        Result<T>.Fail(ErrorCodes.GenForbidden, "No tiene permiso para esta operación sobre citas.");

    private static Result<T> FieldError<T>(string field, string message, string? code = null) =>
        Result<T>.Fail(
            ErrorCodes.GenValidationFailed,
            message,
            new List<ApiErrorDetail>
            {
                new() { Field = field, Code = code ?? ErrorCodes.GenValidationFailed, Message = message },
            });
}
