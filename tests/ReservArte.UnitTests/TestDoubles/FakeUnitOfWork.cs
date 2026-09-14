using ReservArte.Application.Common;
using ReservArte.Application.Interfaces;

namespace ReservArte.UnitTests;

/// <summary>
/// Doble de <see cref="IUnitOfWork"/> para tests con mocks: ejecuta la
/// operación tal cual y registra si la transacción real se habría confirmado o
/// deshecho, con las mismas reglas que EfUnitOfWork (éxito → confirmar; fallo o
/// excepción → deshacer). Que la reversión funcione DE VERDAD lo prueba
/// EmployeeAtomicityTests contra SQLite.
/// </summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public bool Committed { get; private set; }

    public bool RolledBack { get; private set; }

    public async Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await operation(cancellationToken);

            if (result.Success)
            {
                Committed = true;
            }
            else
            {
                RolledBack = true;
            }

            return result;
        }
        catch
        {
            RolledBack = true;
            throw;
        }
    }
}
