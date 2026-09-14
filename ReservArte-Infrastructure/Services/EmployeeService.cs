using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReservArte.Infrastructure.Options;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Shared.Api;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Casos de uso del módulo de Empleados.
///
/// Un empleado y su cuenta de acceso son la misma fila lógica: `Employee.Id`
/// ES `User.Id` (clave primaria compartida). Por eso el alta crea primero el
/// usuario de Identity y después la ficha, y la edición propaga los cambios a
/// ambos: si se desincronizan, el empleado deja de poder entrar.
///
/// Quién entra al módulo lo decide el `[Authorize(Roles)]` del controlador;
/// aquí se aplican las reglas que dependen de los datos (RA-869d7ezz4): el
/// rol del empleado afectado y si es el propio llamador.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;
    private readonly AppOptions _appOptions;
    private readonly IMapper _mapper;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork,
        UserManager<User> userManager,
        ICurrentOrganizationService currentOrganization,
        ICurrentUserService currentUser,
        IEmailService emailService,
        IOptions<AppOptions> appOptions,
        IMapper mapper,
        ILogger<EmployeeService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _emailService = emailService;
        _appOptions = appOptions.Value;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Solo un Admin asigna el rol Admin o gestiona a otro Admin: si un
    /// Manager pudiera, podría fabricarse privilegios que no tiene. Falla
    /// cerrado: un rol ausente o desconocido cuenta como no-Admin.
    /// </summary>
    private bool CallerIsAdmin => _currentUser.Role == Roles.Admin;

    private bool IsCaller(int employeeId) => _currentUser.UserId == employeeId;

    public async Task<Result<PagedResult<EmployeeDto>>> GetPagedAsync(
        EmployeeFilter filter, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is null)
        {
            return Result<PagedResult<EmployeeDto>>.Fail(
                ErrorCodes.OrgTenantNotResolved,
                "No se ha podido resolver la organización de la petición.");
        }

        var page = await _repository.GetPagedAsync(filter, cancellationToken);

        return Result<PagedResult<EmployeeDto>>.Ok(new PagedResult<EmployeeDto>
        {
            Items = page.Items.Select(_mapper.Map<EmployeeDto>).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize,
        });
    }

    public async Task<Result<EmployeeDto>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);

        return employee is null
            ? NotFound(id)
            : Result<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(employee));
    }

    public async Task<Result<EmployeeDto>> CreateAsync(
        CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is not { } organizationId)
        {
            return Result<EmployeeDto>.Fail(
                ErrorCodes.OrgTenantNotResolved,
                "No se ha podido resolver la organización de la petición.");
        }

        if (request.Rol == Roles.Admin && !CallerIsAdmin)
        {
            return AdminRoleForbidden();
        }

        var email = request.Email.Trim();

        // El índice único de Employees.Email es global, así que se comprueba
        // antes de tocar Identity: así el conflicto se devuelve como
        // GEN_CONFLICT y no como un error de base de datos.
        if (await _repository.EmailExistsAsync(email, cancellationToken: cancellationToken))
        {
            return EmailConflict();
        }

        User? createdUser = null;

        // Cuenta y ficha en UNA transacción (RA-869f1811u): si la ficha no se
        // guarda, la cuenta recién creada se deshace con ella. Sustituye a la
        // compensación manual con DeleteAsync, que no cubría un fallo del propio
        // borrado. Las entidades se construyen DENTRO de la operación: con
        // reintentos de conexión puede ejecutarse más de una vez, y cada intento
        // debe empezar de cero.
        var result = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var user = new User
            {
                OrganizationId = organizationId,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                UserName = email,
                Email = email,
                PhoneNumber = request.Phone,
                Rol = request.Rol,
                ProfileImageUrl = request.ProfileImageUrl,
            };

            // Sin contraseña: el empleado la crea desde la invitación
            // (RA-869f17y68). Quien da el alta nunca conoce la credencial.
            var identityResult = await _userManager.CreateAsync(user);

            if (!identityResult.Succeeded)
            {
                return IdentityFailure(
                    identityResult, "El alta del empleado no supera las validaciones.");
            }

            var employee = new Employee
            {
                // Clave compartida: la ficha toma el Id que Identity acaba de
                // asignar al usuario.
                Id = user.Id,
                OrganizationId = organizationId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = email,
                Phone = request.Phone,
                Rol = request.Rol,
                ProfileImageUrl = request.ProfileImageUrl,
                HireDate = request.HireDate,
                IsActive = true,
            };

            _repository.Add(employee);
            await _repository.SaveChangesAsync(ct);

            createdUser = user;

            return Result<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(employee));
        }, cancellationToken);

        if (!result.Success)
        {
            return result;
        }

        _logger.LogInformation(
            "Empleado {EmployeeId} creado en la organización {OrganizationId}",
            result.Data!.Id, organizationId);

        // La invitación sale DESPUÉS de confirmar, nunca dentro de la
        // transacción. Un correo enviado antes del commit podría invitar a una
        // cuenta que la transacción acaba deshaciendo, y un reintento de
        // conexión lo mandaría dos veces. Si el envío falla, el alta se
        // mantiene y la invitación es reenviable.
        await SendInvitationEmailAsync(createdUser!, cancellationToken);

        return result;
    }

    public async Task<Result<EmployeeDto>> UpdateAsync(
        int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);

        if (employee is null)
        {
            return NotFound(id);
        }

        if (!CallerIsAdmin && (employee.Rol == Roles.Admin || request.Rol == Roles.Admin))
        {
            return AdminRoleForbidden();
        }

        // Ni siquiera un Admin cambia su propio rol: promocionarse es una
        // escalada, y degradarse puede dejar a la organización sin nadie que
        // la gestione. Editar el resto de su ficha sí está permitido.
        if (IsCaller(id) && request.Rol != employee.Rol)
        {
            return Forbidden("No puedes cambiar tu propio rol.");
        }

        var email = request.Email.Trim();

        if (await _repository.EmailExistsAsync(email, id, cancellationToken))
        {
            return EmailConflict();
        }

        // Ficha y cuenta en UNA transacción, comprobando CADA resultado de
        // Identity (RA-869f1811u). Antes los resultados se ignoraban. Si Identity
        // rechazaba el email (p. ej. de una cuenta que no es empleado, que
        // EmailExistsAsync no ve), ya había cambiado el usuario en memoria; el
        // guardado final de la ficha, sobre el mismo contexto, persistía ese
        // cambio rechazado. Resultado: 200 OK con la ficha y la cuenta
        // apuntando a un email ajeno (reproducido en runtime).
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            employee.FirstName = request.FirstName.Trim();
            employee.LastName = request.LastName.Trim();
            employee.Email = email;
            employee.Phone = request.Phone;
            employee.Rol = request.Rol;
            employee.ProfileImageUrl = request.ProfileImageUrl;
            employee.HireDate = request.HireDate;

            _repository.Update(employee);

            // La cuenta de acceso va en paralelo: si la ficha cambia de email o
            // de rol y el usuario no, el empleado entraría con datos obsoletos
            // (y el rol viaja en el JWT).
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user is not null)
            {
                user.FirstName = employee.FirstName;
                user.LastName = employee.LastName;
                user.PhoneNumber = employee.Phone;
                user.Rol = employee.Rol;
                user.ProfileImageUrl = employee.ProfileImageUrl;
                user.UpdatedAt = DateTime.UtcNow;

                // SetEmailAsync mantiene el email normalizado que usa el login.
                // Se encadenan: en cuanto uno falla no se sigue.
                var identityResult = await _userManager.SetEmailAsync(user, email);

                if (identityResult.Succeeded)
                {
                    identityResult = await _userManager.SetUserNameAsync(user, email);
                }

                if (identityResult.Succeeded)
                {
                    identityResult = await _userManager.UpdateAsync(user);
                }

                if (!identityResult.Succeeded)
                {
                    return IdentityFailure(
                        identityResult, "La edición del empleado no supera las validaciones.");
                }
            }
            else
            {
                _logger.LogWarning(
                    "El empleado {EmployeeId} no tiene usuario de Identity asociado", id);
            }

            await _repository.SaveChangesAsync(ct);

            return Result<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(employee));
        }, cancellationToken);
    }

    public Task<Result<EmployeeDto>> DeactivateAsync(
        int id, CancellationToken cancellationToken = default) =>
        SetActiveAsync(id, isActive: false, cancellationToken);

    public Task<Result<EmployeeDto>> ReactivateAsync(
        int id, CancellationToken cancellationToken = default) =>
        SetActiveAsync(id, isActive: true, cancellationToken);

    private async Task<Result<EmployeeDto>> SetActiveAsync(
        int id, bool isActive, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);

        if (employee is null)
        {
            return NotFound(id);
        }

        if (!CallerIsAdmin && employee.Rol == Roles.Admin)
        {
            return AdminRoleForbidden();
        }

        // La baja bloquea la cuenta al instante (RA-869f180e5): darse de baja
        // a uno mismo dejaría fuera al llamador sin nadie que lo deshaga.
        if (!isActive && IsCaller(id))
        {
            return Forbidden("No puedes darte de baja a ti mismo.");
        }

        // Idempotente: desactivar a quien ya está de baja no es un error, pero
        // tampoco debe sellar UpdatedAt como si algo hubiera cambiado.
        if (employee.IsActive == isActive)
        {
            return Result<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(employee));
        }

        // Ficha y bloqueo de la cuenta en UNA transacción (RA-869f1811u). Antes
        // la ficha se guardaba primero y el bloqueo iba después: si el bloqueo
        // fallaba, quedaba un empleado «de baja» que seguía entrando.
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            employee.IsActive = isActive;
            _repository.Update(employee);
            await _repository.SaveChangesAsync(ct);

            // La ficha por sí sola no cierra el acceso: AuthService valida
            // contra AspNetUsers y no mira Employee.IsActive (RA-869f180e5).
            if (!await SyncAccountLockAsync(id, isActive))
            {
                return Result<EmployeeDto>.Fail(
                    ErrorCodes.GenInternalError,
                    "No se pudo actualizar el acceso de la cuenta; la operación se ha deshecho.");
            }

            _logger.LogInformation(
                "Empleado {EmployeeId} {Accion}", id, isActive ? "reactivado" : "desactivado");

            return Result<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(employee));
        }, cancellationToken);
    }

    /// <summary>
    /// Propaga la baja o el alta a la cuenta de Identity mediante el lockout:
    /// bloqueo permanente al desactivar, y retirada del bloqueo al reactivar.
    /// Devuelve si se pudo aplicar. Sin cuenta asociada no hay nada que
    /// sincronizar y no se trata como fallo (comportamiento previo). El access
    /// token ya emitido sigue siendo válido hasta caducar (límite conocido,
    /// RA-869f180e5); lo que esto impide es abrir sesión nueva y renovarla.
    /// </summary>
    private async Task<bool> SyncAccountLockAsync(int employeeId, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(employeeId.ToString());

        if (user is null)
        {
            _logger.LogWarning(
                "El empleado {EmployeeId} no tiene usuario de Identity: no se puede " +
                "sincronizar el bloqueo de la cuenta", employeeId);
            return true;
        }

        // IsLockedOutAsync solo considera LockoutEnd si LockoutEnabled está
        // activo, de ahí que se asegure primero.
        var result = await _userManager.SetLockoutEnabledAsync(user, true);

        if (result.Succeeded)
        {
            result = await _userManager.SetLockoutEndDateAsync(
                user, isActive ? null : DateTimeOffset.MaxValue);
        }

        if (!result.Succeeded)
        {
            _logger.LogError(
                "No se pudo sincronizar el bloqueo de la cuenta del empleado {EmployeeId}: {Errores}",
                employeeId, string.Join("; ", result.Errors.Select(e => e.Code)));
        }

        return result.Succeeded;
    }

    // ── Invitación (RA-869f17y68) ─────────────────────────────────────────

    public async Task<Result<EmployeeDto>> ResendInvitationAsync(
        int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return NotFound(employeeId);
        }

        if (!CallerIsAdmin && employee.Rol == Roles.Admin)
        {
            return AdminRoleForbidden();
        }

        // Un empleado de baja tiene la cuenta bloqueada: invitarle a entrar
        // sería mandarle a una puerta cerrada.
        if (!employee.IsActive)
        {
            return Result<EmployeeDto>.Fail(
                ErrorCodes.GenConflict, "El empleado está dado de baja.");
        }

        var user = await _userManager.FindByIdAsync(employeeId.ToString());

        if (user is null)
        {
            _logger.LogWarning(
                "El empleado {EmployeeId} no tiene usuario de Identity: no hay a quién invitar",
                employeeId);

            return NotFound(employeeId);
        }

        // Solo para cuentas sin credencial. A quien ya la tiene, reenviarle una
        // invitación sería mandarle un cambio de contraseña que no ha pedido;
        // para eso está /auth/forgot-password.
        if (await _userManager.HasPasswordAsync(user))
        {
            return Result<EmployeeDto>.Fail(
                ErrorCodes.GenConflict, "El empleado ya ha establecido su contraseña.");
        }

        if (!await SendInvitationEmailAsync(user, cancellationToken))
        {
            return Result<EmployeeDto>.Fail(
                ErrorCodes.GenInternalError,
                "No se pudo enviar la invitación. Inténtalo de nuevo.");
        }

        return Result<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(employee));
    }

    /// <summary>
    /// Envía la invitación para establecer la contraseña. Devuelve si se pudo
    /// enviar: en el alta el resultado se ignora a propósito (el empleado ya
    /// está creado y no se revierte por un fallo de correo), mientras que el
    /// reenvío sí lo reporta, porque enviarlo es justo lo que se ha pedido.
    /// El token NUNCA se registra en logs.
    /// </summary>
    private async Task<bool> SendInvitationEmailAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _userManager.GenerateUserTokenAsync(
                user, InvitationTokenDefaults.ProviderName, InvitationTokenDefaults.Purpose);

            // El token puede llevar caracteres no seguros para URL (+, /, =).
            var link = $"{_appOptions.FrontendBaseUrl}/set-password/{Uri.EscapeDataString(token)}";

            await _emailService.SendAsync(
                new EmailMessage
                {
                    To = user.Email!,
                    Subject = "Te damos la bienvenida: crea tu contraseña",
                    Body =
                        $"Hola {user.FirstName},\n\n" +
                        "Se ha creado tu cuenta de acceso. Para empezar a usarla, crea tu " +
                        "contraseña desde este enlace:\n\n" +
                        $"{link}\n\n" +
                        "El enlace caduca en 7 días. Si ha caducado, pide que te reenvíen la " +
                        "invitación.\n\n" +
                        "Si no esperabas este mensaje, ignóralo.",
                    IsHtml = false,
                },
                cancellationToken);

            _logger.LogInformation("Invitación enviada al empleado {EmployeeId}", user.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No se pudo enviar la invitación al empleado {EmployeeId}; la cuenta queda creada",
                user.Id);

            return false;
        }
    }

    // ── Disponibilidad (RA-869d7f01b) ─────────────────────────────────────

    /// <summary>
    /// Rango de ausencias por defecto: desde hoy y 90 días. Sin un rango, la
    /// consulta crecería sin tope con el histórico futuro del empleado.
    /// </summary>
    private const int DefaultExceptionDays = 90;

    public async Task<Result<EmployeeAvailabilityResponse>> GetAvailabilityAsync(
        int employeeId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return NotFound<EmployeeAvailabilityResponse>(employeeId);
        }

        var (rangeFrom, rangeTo) = ResolveExceptionRange(from, to);

        if (rangeTo < rangeFrom)
        {
            return Result<EmployeeAvailabilityResponse>.Fail(
                ErrorCodes.GenValidationFailed,
                "El fin del rango no puede ser anterior a su inicio.",
                new[]
                {
                    new ApiErrorDetail
                    {
                        Field = "to",
                        Code = "InvalidRange",
                        Message = "El fin del rango no puede ser anterior a su inicio.",
                    },
                });
        }

        return Result<EmployeeAvailabilityResponse>.Ok(
            await BuildAvailabilityAsync(employeeId, rangeFrom, rangeTo, cancellationToken));
    }

    public async Task<Result<EmployeeAvailabilityResponse>> ReplaceAvailabilityAsync(
        int employeeId,
        UpdateAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return NotFound<EmployeeAvailabilityResponse>(employeeId);
        }

        if (!CallerIsAdmin && employee.Rol == Roles.Admin)
        {
            return AdminRoleForbidden<EmployeeAvailabilityResponse>();
        }

        // Ni el Id del tramo ni el empleado ni el tenant se toman del payload:
        // el repositorio los impone al reemplazar.
        var slots = request.WeeklySchedule
            .Select(slot => new EmployeeAvailability
            {
                DayOfWeek = slot.DayOfWeek,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                IsRecurring = slot.IsRecurring,
                IsActive = true,
            })
            .ToList();

        await _repository.ReplaceAvailabilitiesAsync(employeeId, slots, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Horario semanal del empleado {EmployeeId} reemplazado por {Tramos} tramos",
            employeeId, slots.Count);

        var (rangeFrom, rangeTo) = ResolveExceptionRange(from: null, to: null);

        return Result<EmployeeAvailabilityResponse>.Ok(
            await BuildAvailabilityAsync(employeeId, rangeFrom, rangeTo, cancellationToken));
    }

    public async Task<Result<EmployeeExceptionDto>> AddExceptionAsync(
        int employeeId,
        CreateEmployeeExceptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is not { } organizationId)
        {
            return Result<EmployeeExceptionDto>.Fail(
                ErrorCodes.OrgTenantNotResolved,
                "No se ha podido resolver la organización de la petición.");
        }

        var employee = await _repository.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return NotFound<EmployeeExceptionDto>(employeeId);
        }

        if (!CallerIsAdmin && employee.Rol == Roles.Admin)
        {
            return AdminRoleForbidden<EmployeeExceptionDto>();
        }

        var exception = new EmployeeException
        {
            EmployeeId = employeeId,
            OrganizationId = organizationId,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Type = request.Type,
            Reason = request.Reason?.Trim(),
            IsActive = true,
        };

        _repository.AddException(exception);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Ausencia {ExceptionId} registrada para el empleado {EmployeeId}",
            exception.Id, employeeId);

        return Result<EmployeeExceptionDto>.Ok(_mapper.Map<EmployeeExceptionDto>(exception));
    }

    public async Task<Result<EmployeeExceptionDto>> DeleteExceptionAsync(
        int employeeId,
        int exceptionId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return NotFound<EmployeeExceptionDto>(employeeId);
        }

        if (!CallerIsAdmin && employee.Rol == Roles.Admin)
        {
            return AdminRoleForbidden<EmployeeExceptionDto>();
        }

        var exception = await _repository.GetExceptionAsync(
            employeeId, exceptionId, cancellationToken);

        if (exception is null)
        {
            return Result<EmployeeExceptionDto>.Fail(
                ErrorCodes.GenNotFound,
                $"No existe la ausencia con id {exceptionId} para este empleado.");
        }

        // Idempotente, como la baja de la ficha: retirar algo ya retirado no
        // es un error, pero tampoco debe sellar UpdatedAt de nuevo.
        if (exception.IsActive)
        {
            exception.IsActive = false;
            _repository.UpdateException(exception);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Result<EmployeeExceptionDto>.Ok(_mapper.Map<EmployeeExceptionDto>(exception));
    }

    private static (DateTime From, DateTime To) ResolveExceptionRange(DateTime? from, DateTime? to)
    {
        var rangeFrom = from ?? DateTime.UtcNow.Date;

        return (rangeFrom, to ?? rangeFrom.AddDays(DefaultExceptionDays));
    }

    private async Task<EmployeeAvailabilityResponse> BuildAvailabilityAsync(
        int employeeId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetAvailabilitiesAsync(employeeId, cancellationToken);
        var exceptions = await _repository.GetExceptionsAsync(
            employeeId, from, to, cancellationToken);

        return new EmployeeAvailabilityResponse
        {
            EmployeeId = employeeId,
            WeeklySchedule = schedule.Select(_mapper.Map<EmployeeAvailabilityDto>).ToList(),
            Exceptions = exceptions.Select(_mapper.Map<EmployeeExceptionDto>).ToList(),
            ExceptionsFrom = from,
            ExceptionsTo = to,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Mismo resultado para «no existe» y «es de otra organización»: el
    /// repositorio ya acota al tenant, y distinguirlos revelaría qué ids
    /// existen en otras organizaciones.
    /// </summary>
    private static Result<T> NotFound<T>(int id) =>
        Result<T>.Fail(ErrorCodes.GenNotFound, $"No existe el empleado con id {id}.");

    private static Result<EmployeeDto> NotFound(int id) => NotFound<EmployeeDto>(id);

    private static Result<T> Forbidden<T>(string message) =>
        Result<T>.Fail(ErrorCodes.GenForbidden, message);

    private static Result<EmployeeDto> Forbidden(string message) => Forbidden<EmployeeDto>(message);

    private static Result<T> AdminRoleForbidden<T>() =>
        Forbidden<T>("Solo un administrador puede asignar el rol Admin o gestionar a otro administrador.");

    private static Result<EmployeeDto> AdminRoleForbidden() => AdminRoleForbidden<EmployeeDto>();

    /// <summary>
    /// Traduce un rechazo de Identity. Email o nombre de usuario duplicado →
    /// GEN_CONFLICT: puede chocar con una cuenta que no es empleado (un
    /// cliente, un admin sin ficha), que EmailExistsAsync no ve porque solo
    /// mira Employees. El resto → GEN_VALIDATION_FAILED con detalle.
    /// </summary>
    private static Result<EmployeeDto> IdentityFailure(IdentityResult identityResult, string message)
    {
        if (identityResult.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            return EmailConflict();
        }

        var details = identityResult.Errors
            .Select(e => new ApiErrorDetail
            {
                Field = "email",
                Code = e.Code,
                Message = e.Description,
            })
            .ToList();

        return Result<EmployeeDto>.Fail(ErrorCodes.GenValidationFailed, message, details);
    }

    private static Result<EmployeeDto> EmailConflict() =>
        Result<EmployeeDto>.Fail(
            ErrorCodes.GenConflict, "Ya existe una cuenta con ese email.");
}
