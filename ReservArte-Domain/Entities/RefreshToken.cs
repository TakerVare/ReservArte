namespace ReservArte.Domain.Entities;

/// <summary>
/// Refresh token persistido y revocable (vol. 2 §9.2.2). Adaptado al User
/// de Identity (UserId int). El valor Token es opaco (64 bytes base64,
/// generado por JwtTokenService.GenerateRefreshToken), no un JWT.
/// La rotación revoca el token usado y crea uno nuevo en cada refresh.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRevoked { get; set; }

    public string? CreatedByIp { get; set; }

    public User User { get; set; } = null!;
}