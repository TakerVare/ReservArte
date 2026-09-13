using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
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
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly IMapper _mapper;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IEmployeeRepository repository,
        UserManager<User> userManager,
        ICurrentOrganizationService currentOrganization,
        IMapper mapper,
        ILogger<EmployeeService> logger)
    {
        _repository = repository;
        _userManager = userManager;
        _currentOrganization = currentOrganization;
        _mapper = mapper;
        _logger = logger;
    }

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

        var email = request.Email.Trim();

        // El índice único de Employees.Email es global, así que se comprueba
        // antes de tocar Identity: así el conflicto se devuelve como
        // GEN_CONFLICT y no como un error de base de datos.
        if (await _repository.EmailExistsAsync(email, cancellationToken: cancellationToken))
        {
            return EmailConflict();
        }

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

        // Sin contraseña: el empleado la establece con /auth/forgot-password.
        // Quien da el alta nunca conoce la credencial. La cuenta queda sin
        // acceso local hasta entonces, que es el mismo estado que ya tienen
        // las cuentas creadas por login social.
        var identityResult = await _userManager.CreateAsync(user);

        if (!identityResult.Succeeded)
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

            return Result<EmployeeDto>.Fail(
                ErrorCodes.GenValidationFailed,
                "El alta del empleado no supera las validaciones.",
                details);
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

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Sin transacción compartida con Identity: si la ficha falla, el
            // usuario ya creado quedaría huérfano y bloquearía el email para
            // siempre. Se deshace antes de propagar.
            await _userManager.DeleteAsync(user);
            _logger.LogError(
                "Fallo al crear la ficha del empleado {UserId}; se revierte el usuario",
                user.Id);
            throw;
        }

        _logger.LogInformation(
            "Empleado {EmployeeId} creado en la organización {OrganizationId}",
            employee.Id, organizationId);

        return Result<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(employee));
    }

    public async Task<Result<EmployeeDto>> UpdateAsync(
        int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _repository.GetByIdAsync(id, cancellationToken);

        if (employee is null)
        {
            return NotFound(id);
        }

        var email = request.Email.Trim();

        if (await _repository.EmailExistsAsync(email, id, cancellationToken))
        {
            return EmailConflict();
        }

        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = email;
        employee.Phone = request.Phone;
        employee.Rol = request.Rol;
        employee.ProfileImageUrl = request.ProfileImageUrl;
        employee.HireDate = request.HireDate;

        _repository.Update(employee);

        // La cuenta de acceso va en paralelo: si la ficha cambia de email o de
        // rol y el usuario no, el empleado entraría con datos obsoletos (y el
        // rol viaja en el JWT).
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
            await _userManager.SetEmailAsync(user, email);
            await _userManager.SetUserNameAsync(user, email);
            await _userManager.UpdateAsync(user);
        }
        else
        {
            _logger.LogWarning(
                "El empleado {EmployeeId} no tiene usuario de Identity asociado", id);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(employee));
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

        // Idempotente: desactivar a quien ya está de baja no es un error, pero
        // tampoco debe sellar UpdatedAt como si algo hubiera cambiado.
        if (employee.IsActive == isActive)
        {
            return Result<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(employee));
        }

        employee.IsActive = isActive;
        _repository.Update(employee);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Empleado {EmployeeId} {Accion}", id, isActive ? "reactivado" : "desactivado");

        return Result<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(employee));
    }

    /// <summary>
    /// Mismo resultado para «no existe» y «es de otra organización»: el
    /// repositorio ya acota al tenant, y distinguirlos revelaría qué ids
    /// existen en otras organizaciones.
    /// </summary>
    private static Result<EmployeeDto> NotFound(int id) =>
        Result<EmployeeDto>.Fail(
            ErrorCodes.GenNotFound, $"No existe el empleado con id {id}.");

    private static Result<EmployeeDto> EmailConflict() =>
        Result<EmployeeDto>.Fail(
            ErrorCodes.GenConflict, "Ya existe una cuenta con ese email.");
}
