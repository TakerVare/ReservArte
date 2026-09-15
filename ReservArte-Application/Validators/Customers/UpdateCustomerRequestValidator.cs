using FluentValidation;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Validators.Customers;

/// <summary>
/// Validación de entrada de la edición de cliente. Mismas reglas de datos que el
/// alta; el conflicto de email lo resuelve CustomerService.
/// </summary>
public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
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

        RuleFor(x => x.BirthDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("La fecha de nacimiento no puede ser futura.")
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.ProfileImageUrl)
            .MaximumLength(500).WithMessage("La URL de la imagen no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl));

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("La categoría es obligatoria.")
            .Must(category => CustomerCategories.All.Contains(category))
                .WithMessage($"La categoría debe ser una de: {string.Join(", ", CustomerCategories.All)}.");

        RuleFor(x => x.PreferredContactMethod)
            .NotEmpty().WithMessage("El canal de contacto es obligatorio.")
            .Must(method => CustomerContactMethods.All.Contains(method))
                .WithMessage($"El canal de contacto debe ser uno de: {string.Join(", ", CustomerContactMethods.All)}.");
    }
}
