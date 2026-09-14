using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ReservArte.Application.Common;
using ReservArte.Application.Interfaces;

namespace ReservArte.Infrastructure.Persistence;

/// <summary>
/// Unidad de trabajo sobre el <see cref="AppDbContext"/> de la petición.
///
/// Cubre también a Identity: el <c>UserManager</c> trabaja sobre el MISMO
/// contexto scoped, así que sus <c>SaveChanges</c> internos participan en la
/// transacción abierta aquí y se deshacen con ella (lo prueba
/// EmployeeAtomicityTests contra SQLite real).
/// </summary>
public class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public EfUnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        // La conexión usa EnableRetryOnFailure, y con una estrategia de
        // reintentos EF Core RECHAZA una transacción abierta a mano: hay que
        // abrirla dentro de la estrategia, que reejecuta el bloque completo si
        // la conexión cae a mitad.
        var strategy = _context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(
            async ct =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(ct);

                try
                {
                    var result = await operation(ct);

                    if (result.Success)
                    {
                        await transaction.CommitAsync(ct);
                    }
                    else
                    {
                        await RollbackAsync(transaction);
                    }

                    return result;
                }
                catch
                {
                    await RollbackAsync(transaction);
                    throw;
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Deshacer en la base de datos no basta. Los cambios siguen marcados en el
    /// change tracker del contexto compartido, y cualquier <c>SaveChanges</c>
    /// posterior en la misma petición los volvería a escribir. Es exactamente
    /// lo que pasaba antes: Identity rechazaba un email duplicado, pero ya había
    /// modificado el usuario en memoria, y el guardado de la ficha lo persistía.
    /// </summary>
    private async Task RollbackAsync(IDbContextTransaction transaction)
    {
        try
        {
            // Sin token de cancelación a propósito: si la petición se cancela,
            // la reversión debe completarse igualmente.
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch
        {
            // Conexión rota: la transacción ya no existe en el servidor y SQL
            // Server la ha deshecho por su cuenta. No se enmascara el error
            // original con este.
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }
}
