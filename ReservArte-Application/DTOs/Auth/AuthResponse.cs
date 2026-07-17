namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Respuesta de un login/registro exitoso: par de tokens + datos del
/// usuario. Es el "data" del envelope. Si el usuario tuviera 2FA activa,
/// en su tarea (RA-869d7ezgy) se devolverá en su lugar un estado
/// intermedio mfa_required en vez de este objeto.
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public UserDto User { get; set; } = null!;
}

/// <summary>Datos públicos del usuario autenticado (nunca incluye hash ni secretos).</summary>
public class UserDto
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;
}