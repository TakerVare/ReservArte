namespace ReservArte.Domain.Common;

/// <summary>
/// Forma canónica de un email (H-37, RA-869f8pmpa): sin espacios alrededor y en minúsculas.
/// PostgreSQL distingue mayúsculas al comparar texto, así que la unicidad por email de Clientes y
/// Empleados y sus búsquedas de duplicados solo son fiables si el email se guarda y se busca siempre
/// en esta forma. Un CHECK de minúsculas en Customers y Employees lo refuerza en la base de datos.
/// Las cuentas de Identity usan además sus columnas normalizadas (NormalizedEmail).
/// </summary>
public static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
