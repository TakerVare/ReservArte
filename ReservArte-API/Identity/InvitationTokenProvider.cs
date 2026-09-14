using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ReservArte.Application.Common;

namespace ReservArte.API.Identity;

/// <summary>
/// Opciones del proveedor de invitación. Derivar de
/// <see cref="DataProtectionTokenProviderOptions"/> permite darle una caducidad
/// propia sin alterar la del resto de tokens de Identity, que la comparten
/// (reset de contraseña incluido, que sigue en 1 día).
/// </summary>
public class InvitationTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public InvitationTokenProviderOptions()
    {
        Name = "InvitationTokenProvider";
        TokenLifespan = InvitationTokenDefaults.Lifespan;
    }
}

/// <summary>
/// Proveedor de tokens de invitación (RA-869f17y68): mismo mecanismo que el de
/// recuperación —Data Protection, firmado con el security stamp del usuario—,
/// pero con caducidad propia. Ir firmado con el security stamp es justo lo que
/// hace que el enlace deje de valer en cuanto el empleado establece su
/// contraseña.
///
/// Vive en la capa de API porque <see cref="DataProtectorTokenProvider{TUser}"/>
/// viene del framework compartido de ASP.NET Core, que Infrastructure no
/// referencia. Las constantes del contrato están en Application.
/// </summary>
public class InvitationTokenProvider<TUser> : DataProtectorTokenProvider<TUser>
    where TUser : class
{
    public InvitationTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<InvitationTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<TUser>> logger)
        : base(dataProtectionProvider, options, logger)
    {
    }
}
