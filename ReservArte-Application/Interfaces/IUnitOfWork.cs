using ReservArte.Application.Common;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Ejecuta una operación de negocio como una unidad atómica (RA-869f1811u).
///
/// Existe porque varias operaciones escriben en más de un sitio a la vez —la
/// ficha del empleado y su cuenta de Identity— y tienen que salir juntas o no
/// salir. La operación devuelve un <see cref="Result{T}"/>: si es un éxito se
/// confirma; si es un fallo, o si lanza, se deshace todo lo escrito dentro.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Ejecuta <paramref name="operation"/> en una transacción. La operación
    /// PUEDE ejecutarse más de una vez si la conexión sufre un fallo transitorio
    /// (reintentos de la estrategia de ejecución): no debe tener efectos fuera
    /// de la base de datos —enviar correos, por ejemplo— y debe construir sus
    /// entidades dentro, para que cada intento empiece de cero.
    /// </summary>
    Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default);
}
