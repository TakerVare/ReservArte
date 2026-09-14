namespace ReservArte.Application.DTOs.Auth;

/// <summary>
/// Establecimiento de la contraseña desde una invitación de alta
/// (POST /api/v1/auth/set-password, RA-869f17y68). Mismo formato que el
/// restablecimiento, pero el token viene del proveedor «Invitation» (7 días) y
/// solo vale para cuentas que aún no tienen contraseña.
/// </summary>
public class SetPasswordRequest
{
    public string Email { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}
