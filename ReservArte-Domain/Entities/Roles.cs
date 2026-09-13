namespace ReservArte.Domain.Entities;

/// <summary>
/// Catálogo canónico de roles del producto (RA-869f18116). Es la ÚNICA
/// definición válida: el claim `role` del JWT, el CHECK del esquema, los
/// validadores y los `[Authorize(Roles = …)]` deben referenciar estas
/// constantes y nunca literales sueltos.
///
/// **PascalCase, y no es cosmético:** la comparación de roles de
/// `[Authorize(Roles = …)]` distingue mayúsculas, así que un desajuste de
/// casing no da error de compilación — deniega el acceso en silencio.
/// </summary>
public static class Roles
{
    /// <summary>Acceso total.</summary>
    public const string Admin = "Admin";

    /// <summary>Gestión de empleados, servicios y configuración.</summary>
    public const string Manager = "Manager";

    /// <summary>Personal del centro: sus citas y sus clientes.</summary>
    public const string Employee = "Employee";

    /// <summary>Cliente final: solo sus propios datos y reservas.</summary>
    public const string Customer = "Customer";

    /// <summary>Todos los roles válidos.</summary>
    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Admin,
        Manager,
        Employee,
        Customer,
    };

    /// <summary>
    /// Roles asignables a una ficha de empleado. `Customer` queda fuera: un
    /// cliente no es personal del centro y no tiene ficha en `Employees`.
    /// </summary>
    public static readonly IReadOnlyCollection<string> AssignableToEmployee = new[]
    {
        Admin,
        Manager,
        Employee,
    };

    /// <summary>
    /// Rol con el que nace una cuenta creada desde el registro público o por
    /// login social: quien se registra desde la web es un cliente que reserva,
    /// no personal del centro. Elevarlo es una operación explícita del
    /// backoffice.
    /// </summary>
    public const string DefaultForPublicRegistration = Customer;
}
