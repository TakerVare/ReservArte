using System.Reflection;
using AwesomeAssertions;
using ReservArte.Shared.Api;

namespace ReservArte.UnitTests;

/// <summary>
/// Mapa único código de error → status HTTP (RA-869f6r81n). Si alguien añade un
/// código al catálogo sin darle status, este test falla antes de que ese código
/// salga como 500 por la puerta de atrás.
/// </summary>
public class ErrorStatusCodesTests
{
    private static readonly string[] Catalog = typeof(ErrorCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToArray();

    [Fact]
    public void Todo_codigo_del_catalogo_tiene_status_asignado()
    {
        Catalog.Should().NotBeEmpty();
        Catalog.Except(ErrorStatusCodes.MappedCodes).Should().BeEmpty();
    }

    [Fact]
    public void El_mapa_no_tiene_codigos_fuera_del_catalogo()
    {
        ErrorStatusCodes.MappedCodes.Except(Catalog).Should().BeEmpty();
    }

    [Theory]
    [InlineData(ErrorCodes.GenValidationFailed, 400)]
    [InlineData(ErrorCodes.OrgTenantNotResolved, 400)]
    [InlineData(ErrorCodes.AuthMfaInvalid, 400)]
    [InlineData(ErrorCodes.GenUnauthorized, 401)]
    [InlineData(ErrorCodes.AuthInvalidCredentials, 401)]
    [InlineData(ErrorCodes.AuthRefreshInvalid, 401)]
    [InlineData(ErrorCodes.PayRedsysDeclined, 402)]
    [InlineData(ErrorCodes.GenForbidden, 403)]
    [InlineData(ErrorCodes.OrgTenantMismatch, 403)]
    [InlineData(ErrorCodes.CustBlocked, 403)]
    [InlineData(ErrorCodes.GenNotFound, 404)]
    [InlineData(ErrorCodes.GenConflict, 409)]
    [InlineData(ErrorCodes.AptInvalidState, 409)]
    [InlineData(ErrorCodes.AptSlotUnavailable, 409)]
    [InlineData(ErrorCodes.GenRateLimited, 429)]
    [InlineData(ErrorCodes.GenInternalError, 500)]
    public void Cada_codigo_sale_con_el_status_que_documenta_el_catalogo(string code, int expected)
    {
        ErrorStatusCodes.For(code).Should().Be(expected);
    }

    [Theory]
    [InlineData("NO_EXISTE")]
    [InlineData("")]
    [InlineData(null)]
    public void Un_codigo_desconocido_sale_como_500_y_no_como_error_del_cliente(string? code)
    {
        ErrorStatusCodes.For(code).Should().Be(500);
    }
}
