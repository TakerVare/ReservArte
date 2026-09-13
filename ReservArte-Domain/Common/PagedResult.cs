namespace ReservArte.Domain.Common;

/// <summary>
/// Página de resultados más el total de coincidencias. El total se devuelve
/// junto a los elementos porque la capa API lo necesita para `meta.pagination`
/// del envelope, y calcularlo en una segunda llamada duplicaría el filtrado.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>Coincidencias totales del filtro, no de la página.</summary>
    public int TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
