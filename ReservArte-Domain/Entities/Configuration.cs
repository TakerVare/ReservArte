namespace ReservArte.Domain.Entities;

/// <summary>
/// Tabla singleton: siempre una sola fila con Id = 1.
/// Usar UpdateConfiguration() para modificar; nunca insertar con Id distinto de 1.
/// </summary>
public class Configuration
{
    public int Id { get; set; } = 1;
    public string? CompanyName { get; set; }
    public string? CompanyLogo { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
