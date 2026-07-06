using Microsoft.AspNetCore.Identity;

namespace ReservArte.Domain.Entities;

/// <summary>
/// Usuario del sistema sobre ASP.NET Core Identity (IdentityUser&lt;int&gt;).
/// Identity aporta Email, UserName, PasswordHash (NULL para cuentas solo
/// sociales), PhoneNumber, SecurityStamp, TwoFactorEnabled, etc.
/// Aquí viven únicamente los campos de negocio. Tabla real: AspNetUsers
/// (vol. 1 §5, nota Identity + login social).
/// </summary>
public class User : IdentityUser<int>
{
    public Guid OrganizationId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    public string? ProfileImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;

    /// <summary>Perfil de empleada asociado (patrón Employee.Id = User.Id); null si el usuario no es empleada.</summary>
    public Employee? Employee { get; set; }
}