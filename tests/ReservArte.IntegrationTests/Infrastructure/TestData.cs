namespace ReservArte.IntegrationTests.Infrastructure;

public sealed record TestAccount(string Email, string Password, string FirstName);

/// <summary>
/// Datos fijos de la base de tests: el centro A y sus cuentas los crea
/// <c>DevSeeder</c>; el centro B, <see cref="ApiFactory"/>.
/// </summary>
public static class TestData
{
    public static readonly Guid OrgA = new("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");
    public static readonly Guid OrgB = new("11111111-2222-3333-4444-555555555555");

    public static readonly TestAccount AdminA = new("guille@svalero.com", "Admin1234!", "Guillermo");
    public static readonly TestAccount AdminB = new("admin@centrob.test", "AdminB1234!", "Beatriz");
    public static readonly TestAccount CustomerB = new("clienta@centrob.test", "ClienteB123!", "Berta");

    /// <summary>Email único por test, para no chocar con los datos de otros tests.</summary>
    public static string UniqueEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@example.com";
}
