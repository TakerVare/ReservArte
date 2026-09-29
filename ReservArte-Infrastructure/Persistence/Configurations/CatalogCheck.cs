namespace ReservArte.Infrastructure.Persistence.Configurations;

/// <summary>
/// CHECK de una columna de catálogo generado desde las constantes de dominio,
/// para que el esquema no pueda quedarse con un valor distinto del código.
/// Nombre de columna entre comillas dobles: PostgreSQL respeta así el PascalCase (H-37).
/// </summary>
internal static class CatalogCheck
{
    public static string In(string column, IEnumerable<string> values) =>
        $"\"{column}\" IN ({string.Join(", ", values.Select(v => $"'{v}'"))})";
}
