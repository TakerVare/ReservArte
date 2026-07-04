namespace ReservArte.Domain.Entities;

/// <summary>
/// Una fila por organización. Usar UpdateConfiguration() para modificar; nunca insertar más de una fila por OrganizationId.
/// </summary>
public class Configuration
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyLogo { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
