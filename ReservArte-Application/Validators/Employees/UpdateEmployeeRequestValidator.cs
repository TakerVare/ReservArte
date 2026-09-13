using FluentValidation;
using ReservArte.Application.DTOs.Employees;

namespace ReservArte.Application.Validators.Employees;

/// <summary>
/// Mismas reglas que el alta: los campos editables coinciden, y tener dos
/// juegos de reglas divergentes para la misma ficha acabaría permitiendo por
/// edición lo que el alta rechaza.
/// </summary>
public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(100).WithMessage("Los apellidos no pueden superar los 100 caracteres.");

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
            .Must(rol => CreateEmployeeRequestValidator.AllowedRoles.Contains(rol))
                .WithMessage($"El rol debe ser uno de: {string.Join(", ", CreateEmployeeRequestValidator.AllowedRoles)}.");

        RuleFor(x => x.ProfileImageUrl)
            .MaximumLength(500).WithMessage("La URL de la imagen no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl));

        RuleFor(x => x.HireDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("La fecha de alta no puede ser futura.")
            .When(x => x.HireDate.HasValue);
    }
}
