using FluentValidation;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Validators.Employees;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    /// <summary>
    /// Roles admitidos en el alta, tomados del catálogo canónico
    /// (RA-869f18116). Lista blanca y no negra: un rol desconocido debe
    /// fallar, no colarse. `Customer` queda fuera: un cliente no es personal.
    /// </summary>
    public static readonly string[] AllowedRoles = Roles.AssignableToEmployee.ToArray();

    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(100).WithMessage("Los apellidos no pueden superar los 100 caracteres.");

        // Longitudes alineadas con el esquema (Employees.Email NVARCHAR(255),
        // Phone NVARCHAR(20), ProfileImageUrl NVARCHAR(500)): validar aquí
        // evita que el fallo aparezca como error de base de datos.
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(255).WithMessage("El email no puede superar los 255 caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("El teléfono no puede superar los 20 caracteres.")
            .Matches(@"^[+0-9\s().-]+$")
                .WithMessage("El teléfono solo puede contener dígitos y los signos + ( ) . -")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Rol)
            .NotEmpty().WithMessage("El rol es obligatorio.")
            .Must(rol => AllowedRoles.Contains(rol))
                .WithMessage($"El rol debe ser uno de: {string.Join(", ", AllowedRoles)}.");

        RuleFor(x => x.ProfileImageUrl)
            .MaximumLength(500).WithMessage("La URL de la imagen no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl));

        // Una fecha de alta futura es casi siempre un error de tecleo; la del
        // mismo día es válida.
        RuleFor(x => x.HireDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("La fecha de alta no puede ser futura.")
            .When(x => x.HireDate.HasValue);
    }
}
