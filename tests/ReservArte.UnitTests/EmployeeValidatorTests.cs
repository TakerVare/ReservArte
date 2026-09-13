using FluentAssertions;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Application.Validators.Employees;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Reglas de validación del alta y la edición de empleados (RA-869d7ezwy).
/// Las longitudes replican las del esquema, así que estos tests también
/// protegen de que un cambio de DTO se salte los límites de la base de datos.
/// </summary>
public class EmployeeValidatorTests
{
    private readonly CreateEmployeeRequestValidator _createValidator = new();
    private readonly UpdateEmployeeRequestValidator _updateValidator = new();

    private static CreateEmployeeRequest ValidCreateRequest() => new()
    {
        FirstName = "María",
        LastName = "Salas",
        Email = "maria@reservarte.com",
        Phone = "600123123",
        Rol = "employee",
        HireDate = new DateOnly(2026, 1, 15),
    };

    [Fact]
    public void Una_peticion_completa_es_valida()
    {
        _createValidator.Validate(ValidCreateRequest()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void El_nombre_es_obligatorio(string firstName)
    {
        var request = new CreateEmployeeRequest
        {
            FirstName = firstName,
            LastName = "Salas",
            Email = "maria@reservarte.com",
        };

        var result = _createValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.FirstName));
    }

    [Fact]
    public void Los_apellidos_son_obligatorios()
    {
        var request = new CreateEmployeeRequest { FirstName = "María", Email = "m@r.com" };

        var result = _createValidator.Validate(request);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.LastName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("sin-arroba")]
    [InlineData("dos@@arrobas.com")]
    public void El_email_debe_tener_formato_valido(string email)
    {
        var request = new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = email,
        };

        var result = _createValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Email));
    }

    [Fact]
    public void El_email_no_puede_superar_la_longitud_de_la_columna()
    {
        var largo = new string('a', 250) + "@reservarte.com"; // 265 caracteres

        var request = new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = largo,
        };

        _createValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("600123123")]
    [InlineData("+34 600 12 31 23")]
    [InlineData("(34) 600-123.123")]
    public void El_telefono_admite_los_formatos_habituales(string phone)
    {
        var request = new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
            Phone = phone,
        };

        _createValidator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void El_telefono_rechaza_texto()
    {
        var request = new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
            Phone = "llamar al fijo",
        };

        _createValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void El_telefono_es_opcional()
    {
        var request = new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
            Phone = null,
        };

        _createValidator.Validate(request).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("employee")]
    [InlineData("admin")]
    public void El_rol_admite_los_valores_de_la_lista_blanca(string rol)
    {
        var request = new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
            Rol = rol,
        };

        _createValidator.Validate(request).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("superadmin")]
    [InlineData("client")]
    [InlineData("")]
    public void El_rol_rechaza_cualquier_valor_fuera_de_la_lista_blanca(string rol)
    {
        var request = new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
            Rol = rol,
        };

        _createValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void La_fecha_de_alta_no_puede_ser_futura()
    {
        var request = new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        };

        _createValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void La_fecha_de_alta_de_hoy_es_valida()
    {
        var request = new CreateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
        };

        _createValidator.Validate(request).IsValid.Should().BeTrue();
    }

    // ── Edición ───────────────────────────────────────────────────────────

    [Fact]
    public void La_edicion_aplica_las_mismas_reglas_que_el_alta()
    {
        var invalido = new UpdateEmployeeRequest
        {
            FirstName = "",
            LastName = "Salas",
            Email = "no-es-un-email",
            Rol = "superadmin",
        };

        var result = _updateValidator.Validate(invalido);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName)
            .Should().Contain(new[] { "FirstName", "Email", "Rol" });
    }

    [Fact]
    public void Una_edicion_completa_es_valida()
    {
        var request = new UpdateEmployeeRequest
        {
            FirstName = "María",
            LastName = "Salas",
            Email = "maria@reservarte.com",
            Rol = "admin",
        };

        _updateValidator.Validate(request).IsValid.Should().BeTrue();
    }
}
