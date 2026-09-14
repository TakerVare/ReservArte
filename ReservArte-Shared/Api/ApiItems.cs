namespace ReservArte.Shared.Api;

/// <summary>
/// Contenido de `data` en las listas paginadas (volumen 1 §5.1.1): los
/// elementos van en `data.items` y los totales en `meta.pagination`.
/// </summary>
public class ApiItems<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
}
