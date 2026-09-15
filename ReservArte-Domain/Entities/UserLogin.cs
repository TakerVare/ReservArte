using Microsoft.AspNetCore.Identity;

namespace ReservArte.Domain.Entities;

/// <summary>
/// Vínculo de login social (tabla AspNetUserLogins) acotado a la organización
/// (RA-869f1xc0u).
///
/// La misma persona puede tener cuenta en varios centros, y el proveedor
/// (Google, Apple, Meta) le da el mismo sujeto (ProviderKey) en todos. Con la
/// clave de Identity, (LoginProvider, ProviderKey), el segundo centro chocaría al
/// vincular. Por eso la clave incluye la organización, que siempre es la de la
/// cuenta: la rellena <c>OrganizationUserStore</c> al crear el vínculo.
/// </summary>
public class UserLogin : IdentityUserLogin<int>
{
    public Guid OrganizationId { get; set; }
}
