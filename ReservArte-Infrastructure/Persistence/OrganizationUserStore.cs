using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ReservArte.Domain.Entities;

namespace ReservArte.Infrastructure.Persistence;

/// <summary>
/// Store de usuarios de Identity sobre <see cref="AppDbContext"/> con los
/// vínculos de login social por organización (RA-869f1xc0u).
///
/// Solo añade una cosa al store de EF: al vincular un proveedor
/// (<c>AddLoginAsync</c>), el vínculo hereda la organización de la cuenta. Las
/// búsquedas no cambian: <c>FindByLoginAsync</c> consulta AspNetUserLogins a
/// través del query filter de tenant, así que el mismo sujeto del proveedor
/// resuelve la cuenta de la organización de la petición.
/// </summary>
public class OrganizationUserStore
    : UserOnlyStore<User, AppDbContext, int, IdentityUserClaim<int>, UserLogin, IdentityUserToken<int>>
{
    public OrganizationUserStore(AppDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
    }

    protected override UserLogin CreateUserLogin(User user, UserLoginInfo login)
    {
        var userLogin = base.CreateUserLogin(user, login);
        userLogin.OrganizationId = user.OrganizationId;
        return userLogin;
    }
}
