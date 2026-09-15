using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;
using ReservArte.Domain.Interfaces;
using ReservArte.Infrastructure.Options;
using ReservArte.Shared.Api;

namespace ReservArte.Infrastructure.Services;

/// <summary>
/// Casos de uso del módulo de Clientes (RA-869d7f369).
///
/// Como en Empleados, la ficha y la cuenta de acceso comparten clave:
/// `Customer.Id` ES `User.Id`. A diferencia de Empleados, la cuenta puede ser
/// de personal: una empleada puede ser clienta de su centro con la misma cuenta
/// (decisión de producto 2026-09-14). Por eso el servicio distingue siempre
/// entre cuenta solo de cliente y cuenta de personal (`User.Rol`), y en la
/// segunda no toca nada que pertenezca al módulo de Empleados.
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentOrganizationService _currentOrganization;
    private readonly IEmailService _emailService;
    private readonly AppOptions _appOptions;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository repository,
        IUnitOfWork unitOfWork,
        UserManager<User> userManager,
        ICurrentOrganizationService currentOrganization,
        IEmailService emailService,
        IOptions<AppOptions> appOptions,
        IMapper mapper,
        ILogger<CustomerService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _currentOrganization = currentOrganization;
        _emailService = emailService;
        _appOptions = appOptions.Value;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Cuenta de personal: cualquier rol que no sea Customer. Falla cerrado: un
    /// rol vacío o desconocido cuenta como personal, y su cuenta no se toca.
    /// </summary>
    private static bool IsStaffAccount(User user) => user.Rol != Roles.Customer;

    public async Task<Result<PagedResult<CustomerDto>>> GetPagedAsync(
        CustomerFilter filter, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is null)
        {
            return TenantNotResolved<PagedResult<CustomerDto>>();
        }

        var page = await _repository.GetPagedAsync(filter, cancellationToken);

        return Result<PagedResult<CustomerDto>>.Ok(new PagedResult<CustomerDto>
        {
            Items = page.Items.Select(_mapper.Map<CustomerDto>).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize,
        });
    }

    public async Task<Result<CustomerDetailDto>> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetProfileAsync(id, cancellationToken);

        return customer is null
            ? NotFound<CustomerDetailDto>(id)
            : Result<CustomerDetailDto>.Ok(_mapper.Map<CustomerDetailDto>(customer));
    }

    public async Task<Result<CustomerDetailDto>> CreateAsync(
        CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentOrganization.OrganizationId is not { } organizationId)
        {
            return TenantNotResolved<CustomerDetailDto>();
        }

        var grantedConsents = (request.GrantedConsents ?? []).Distinct().ToList();

        // Lo comprueba también el validador, pero el servicio no depende de que
        // lo hayan llamado: un alta sin tratamiento de datos no debe existir
        // (mismo criterio que el registro público, RA-869f1xc2n).
        if (CustomerConsentTypes.Required.Except(grantedConsents).Any())
        {
            return Result<CustomerDetailDto>.Fail(
                ErrorCodes.GenValidationFailed,
                "El consentimiento de tratamiento de datos es obligatorio.",
                new[]
                {
                    new ApiErrorDetail
                    {
                        Field = "grantedConsents",
                        Code = "RequiredConsentMissing",
                        Message = "El consentimiento de tratamiento de datos es obligatorio.",
                    },
                });
        }

        var email = request.Email.Trim();

        // Customers tiene índice único (OrganizationId, Email): se comprueba antes
        // para devolver GEN_CONFLICT y no un error de base de datos.
        if (await _repository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return EmailConflict<CustomerDetailDto>();
        }

        // ¿Ya tiene cuenta en el centro? Entonces se le añade la ficha a esa
        // cuenta (p. ej. una empleada que se hace clienta), no se crea otra. El
        // query filter acota la búsqueda al tenant; la comparación explícita es
        // el mismo cinturón que usa AuthService.
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null && existingUser.OrganizationId != organizationId)
        {
            existingUser = null;
        }

        if (existingUser is not null &&
            await _repository.GetByIdAsync(existingUser.Id, cancellationToken) is not null)
        {
            return Result<CustomerDetailDto>.Fail(
                ErrorCodes.GenConflict, "Esa cuenta ya tiene ficha de cliente.");
        }

        User? invitedUser = null;

        // Cuenta (si hace falta), ficha y consentimientos en UNA transacción
        // (RA-869f1811u). Todo se construye dentro: con reintentos de conexión la
        // operación puede ejecutarse más de una vez.
        var result = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            invitedUser = null;
            var now = DateTime.UtcNow;
            int accountId;

            if (existingUser is null)
            {
                var user = new User
                {
                    OrganizationId = organizationId,
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    UserName = email,
                    Email = email,
                    PhoneNumber = request.Phone,
                    Rol = Roles.Customer,
                    ProfileImageUrl = request.ProfileImageUrl,
                };

                // Sin contraseña: la clienta la crea desde la invitación. Quien da
                // el alta nunca conoce la credencial.
                var identityResult = await _userManager.CreateAsync(user);

                if (!identityResult.Succeeded)
                {
                    return IdentityFailure<CustomerDetailDto>(
                        identityResult, "El alta del cliente no supera las validaciones.");
                }

                accountId = user.Id;
                invitedUser = user;
            }
            else
            {
                // La cuenta existente no se modifica: su nombre, rol y email son
                // de quien la gestiona (Empleados, o la propia persona).
                accountId = existingUser.Id;
            }

            var customer = new Customer
            {
                Id = accountId,
                OrganizationId = organizationId,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Phone = request.Phone,
                BirthDate = request.BirthDate,
                ProfileImageUrl = request.ProfileImageUrl,
                Category = request.Category ?? CustomerCategories.New,
                PreferredContactMethod = request.PreferredContactMethod,
                IsActive = true,
                // Solo lo que la clienta ha aceptado, fechado: el RGPD exige poder
                // demostrar cuándo se otorgó.
                Consents = grantedConsents
                    .Select(type => new CustomerConsent
                    {
                        OrganizationId = organizationId,
                        ConsentType = type,
                        IsGranted = true,
                        GrantedAt = now,
                    })
                    .ToList(),
            };

            _repository.Add(customer);
            await _repository.SaveChangesAsync(ct);

            return Result<CustomerDetailDto>.Ok(_mapper.Map<CustomerDetailDto>(customer));
        }, cancellationToken);

        if (!result.Success)
        {
            return result;
        }

        _logger.LogInformation(
            "Cliente {CustomerId} creado en la organización {OrganizationId} ({Cuenta})",
            result.Data!.Id, organizationId, invitedUser is null ? "cuenta existente" : "cuenta nueva");

        // La invitación sale DESPUÉS de confirmar, y solo para la cuenta recién
        // creada: quien ya tenía cuenta ya sabe entrar. Si el envío falla, el
        // alta se mantiene: la clienta puede usar «he olvidado mi contraseña».
        if (invitedUser is not null)
        {
            await SendInvitationEmailAsync(invitedUser, cancellationToken);
        }

        return result;
    }

    public async Task<Result<CustomerDto>> UpdateAsync(
        int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);

        if (customer is null)
        {
            return NotFound<CustomerDto>(id);
        }

        var email = request.Email.Trim();

        if (await _repository.GetByEmailAsync(email, cancellationToken) is { } holder && holder.Id != id)
        {
            return EmailConflict<CustomerDto>();
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        var staffAccount = user is not null && IsStaffAccount(user);

        // En una cuenta de personal el email de la ficha es el de acceso de una
        // empleada. Cambiarlo desde Clientes permitiría, por ejemplo, que una
        // Employee cambiase el email de acceso de una Admin y le robase la cuenta
        // con «he olvidado mi contraseña». Se cambia desde Empleados.
        if (staffAccount && !string.Equals(email, customer.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Result<CustomerDto>.Fail(
                ErrorCodes.GenForbidden,
                "El email de una cuenta de personal solo se puede cambiar desde Empleados.");
        }

        // Ficha y cuenta en UNA transacción, comprobando cada resultado de
        // Identity (RA-869f1811u): un cambio que Identity rechaza queda en el
        // contexto compartido y el guardado de la ficha lo persistiría.
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            customer.FirstName = request.FirstName.Trim();
            customer.LastName = request.LastName.Trim();
            customer.Phone = request.Phone;
            customer.BirthDate = request.BirthDate;
            customer.ProfileImageUrl = request.ProfileImageUrl;
            customer.Category = request.Category;
            customer.PreferredContactMethod = request.PreferredContactMethod;

            if (!staffAccount)
            {
                customer.Email = email;
            }

            _repository.Update(customer);

            if (user is null)
            {
                _logger.LogWarning(
                    "El cliente {CustomerId} no tiene usuario de Identity asociado", id);
            }
            else if (!staffAccount)
            {
                // Cuenta solo de cliente: la ficha y la cuenta son la misma
                // persona, y el email es el de acceso.
                user.FirstName = customer.FirstName;
                user.LastName = customer.LastName;
                user.PhoneNumber = customer.Phone;
                user.ProfileImageUrl = customer.ProfileImageUrl;
                user.UpdatedAt = DateTime.UtcNow;

                var identityResult = IdentityResult.Success;

                // Solo si cambia: SetEmailAsync marca el email como no confirmado
                // y renueva el security stamp aunque el valor sea el mismo.
                if (!string.Equals(user.Email, email, StringComparison.Ordinal))
                {
                    identityResult = await _userManager.SetEmailAsync(user, email);

                    if (identityResult.Succeeded)
                    {
                        identityResult = await _userManager.SetUserNameAsync(user, email);
                    }
                }

                if (identityResult.Succeeded)
                {
                    identityResult = await _userManager.UpdateAsync(user);
                }

                if (!identityResult.Succeeded)
                {
                    return IdentityFailure<CustomerDto>(
                        identityResult, "La edición del cliente no supera las validaciones.");
                }
            }

            await _repository.SaveChangesAsync(ct);

            return Result<CustomerDto>.Ok(_mapper.Map<CustomerDto>(customer));
        }, cancellationToken);
    }

    public Task<Result<CustomerDto>> DeactivateAsync(
        int id, CancellationToken cancellationToken = default) =>
        SetActiveAsync(id, isActive: false, cancellationToken);

    public Task<Result<CustomerDto>> ReactivateAsync(
        int id, CancellationToken cancellationToken = default) =>
        SetActiveAsync(id, isActive: true, cancellationToken);

    /// <summary>
    /// Baja o reactivación de la ficha. A diferencia de Empleados, no toca el
    /// lockout de la cuenta: dar de baja a una clienta no debe dejar sin acceso a
    /// una empleada que también lo es, y el acceso de una clienta no depende de
    /// que su ficha esté activa.
    /// </summary>
    private async Task<Result<CustomerDto>> SetActiveAsync(
        int id, bool isActive, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken);

        if (customer is null)
        {
            return NotFound<CustomerDto>(id);
        }

        // Idempotente: repetir la operación no es un error, pero tampoco debe
        // sellar UpdatedAt como si algo hubiera cambiado.
        if (customer.IsActive != isActive)
        {
            customer.IsActive = isActive;
            _repository.Update(customer);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cliente {CustomerId} {Accion}", id, isActive ? "reactivado" : "dado de baja");
        }

        return Result<CustomerDto>.Ok(_mapper.Map<CustomerDto>(customer));
    }

    /// <summary>
    /// Invitación para crear la contraseña, con el mismo token que la de
    /// empleados (RA-869f17y68): la verifica `POST /auth/set-password`. El
    /// resultado se ignora a propósito, porque el alta ya está confirmada. El
    /// token NUNCA se registra en logs.
    /// </summary>
    private async Task SendInvitationEmailAsync(User user, CancellationToken cancellationToken)
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
                        "Tu centro te ha dado de alta como cliente. Para consultar y reservar " +
                        "tus citas online, crea tu contraseña desde este enlace:\n\n" +
                        $"{link}\n\n" +
                        "El enlace caduca en 7 días. Si ha caducado, entra en la página de " +
                        "acceso y usa «¿Has olvidado tu contraseña?».\n\n" +
                        "Si no esperabas este mensaje, ignóralo.",
                    IsHtml = false,
                },
                cancellationToken);

            _logger.LogInformation("Invitación enviada al cliente {CustomerId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No se pudo enviar la invitación al cliente {CustomerId}; la ficha queda creada",
                user.Id);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Result<T> TenantNotResolved<T>() =>
        Result<T>.Fail(
            ErrorCodes.OrgTenantNotResolved,
            "No se ha podido resolver la organización de la petición.");

    /// <summary>
    /// Mismo resultado para «no existe» y «es de otra organización»: distinguirlos
    /// revelaría qué ids existen en otros centros.
    /// </summary>
    private static Result<T> NotFound<T>(int id) =>
        Result<T>.Fail(ErrorCodes.GenNotFound, $"No existe el cliente con id {id}.");

    private static Result<T> EmailConflict<T>() =>
        Result<T>.Fail(ErrorCodes.GenConflict, "Ya existe un cliente con ese email.");

    /// <summary>
    /// Traduce un rechazo de Identity. Email o nombre de usuario duplicado →
    /// GEN_CONFLICT; el resto → GEN_VALIDATION_FAILED con detalle.
    /// </summary>
    private static Result<T> IdentityFailure<T>(IdentityResult identityResult, string message)
    {
        if (identityResult.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            return Result<T>.Fail(ErrorCodes.GenConflict, "Ya existe una cuenta con ese email.");
        }

        var details = identityResult.Errors
            .Select(e => new ApiErrorDetail
            {
                Field = "email",
                Code = e.Code,
                Message = e.Description,
            })
            .ToList();

        return Result<T>.Fail(ErrorCodes.GenValidationFailed, message, details);
    }
}
