namespace ReservArte.Domain.Interfaces;

/// <summary>
/// Usuario autenticado de la petición actual, leído de los claims del JWT.
/// Lo consumen los servicios de aplicación cuyas reglas dependen de quién
/// llama (p. ej. qué roles puede asignar). En peticiones anónimas, o con un
/// token rechazado, ambas propiedades son null.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Claim `sub`. En el personal coincide con `Employee.Id` (clave compartida).</summary>
    int? UserId { get; }

    /// <summary>Claim `role`, con los valores del catálogo `Roles`.</summary>
    string? Role { get; }
}
