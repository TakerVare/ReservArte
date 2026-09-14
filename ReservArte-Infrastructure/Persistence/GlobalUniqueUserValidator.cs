using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence;

/// <summary>
/// Unicidad GLOBAL de email y nombre de usuario frente a OTRAS organizaciones
/// (RA-869f17vet).
///
/// Con el query filter de tenant sobre AspNetUsers, el <see cref="UserValidator{TUser}"/>
/// por defecto de Identity solo ve la organización de la petición. Pero los
/// índices únicos (<c>EmailIndex</c>, <c>UserNameIndex</c>) son globales: un email
/// ya usado en otra organización pasaría la validación y reventaría al guardar
/// con una violación de índice (500 en vez de 409), en el registro, en el alta
/// de empleado y en el alta por login social.
///
/// Este validador complementa al de Identity sin duplicarlo: solo mira las
/// cuentas de OTRAS organizaciones, saltándose el filtro a propósito, y
/// devuelve los mismos errores (<c>DuplicateEmail</c> / <c>DuplicateUserName</c>)
/// para que el resto del código los trate igual. Solo comprueba existencia:
/// nunca devuelve datos del otro tenant.
/// </summary>
public class GlobalUniqueUserValidator : IUserValidator<User>
{
    private readonly AppDbContext _context;

    public GlobalUniqueUserValidator(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user)
    {
        var errors = new List<IdentityError>();

        var email = await manager.GetEmailAsync(user);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = manager.NormalizeEmail(email);

            if (await UsedByAnotherOrganizationAsync(u => u.NormalizedEmail == normalizedEmail, user))
            {
                errors.Add(manager.ErrorDescriber.DuplicateEmail(email));
            }
        }

        var userName = await manager.GetUserNameAsync(user);

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var normalizedUserName = manager.NormalizeName(userName);

            if (await UsedByAnotherOrganizationAsync(u => u.NormalizedUserName == normalizedUserName, user))
            {
                errors.Add(manager.ErrorDescriber.DuplicateUserName(userName));
            }
        }

        return errors.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(errors.ToArray());
    }

    private Task<bool> UsedByAnotherOrganizationAsync(
        Expression<Func<User, bool>> predicate, User user) =>
        _context.Users
            .IgnoreQueryFilters()
            .Where(predicate)
            .AnyAsync(u => u.Id != user.Id && u.OrganizationId != user.OrganizationId);
}
