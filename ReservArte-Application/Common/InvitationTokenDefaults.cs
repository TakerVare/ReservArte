namespace ReservArte.Application.Common;

/// <summary>
/// Contrato del token de invitación de empleados (RA-869f17y68): nombre del
/// proveedor, propósito y caducidad.
///
/// Vive en Application, y no junto al proveedor, porque lo comparten quien
/// EMITE el token (EmployeeService, al dar de alta o reenviar) y quien lo
/// VERIFICA (AuthService, en set-password), mientras que el proveedor en sí es
/// cableado de ASP.NET Core y vive en la capa de API.
/// </summary>
public static class InvitationTokenDefaults
{
    /// <summary>Nombre con el que se registra el proveedor en Identity.</summary>
    public const string ProviderName = "Invitation";

    /// <summary>
    /// Propósito del token: lo acota a esta operación. Un token emitido para
    /// otro propósito no verifica aquí, aunque venga del mismo proveedor.
    /// </summary>
    public const string Purpose = "SetPassword";

    /// <summary>
    /// Siete días. El token de recuperación (1 día, el de por defecto) se queda
    /// corto para un alta que quizá se atiende al día siguiente o tras un fin
    /// de semana; por eso la invitación tiene proveedor propio y el de
    /// recuperación no se toca.
    /// </summary>
    public static readonly TimeSpan Lifespan = TimeSpan.FromDays(7);
}
