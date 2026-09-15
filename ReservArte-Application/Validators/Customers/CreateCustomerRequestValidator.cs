using FluentValidation;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Validators.Customers;

/// <summary>
/// Validación de entrada del alta de cliente. La unicidad del email no se
/// comprueba aquí sino en CustomerService: un email que ya es de una cuenta del
/// centro no siempre es un conflicto (a una empleada se le añade la ficha), y
/// distinguirlo exige mirar la cuenta, no solo la tabla de clientes.
/// </summary>
public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(100).WithMessage("Los apellidos no pueden superar los 100 caracteres.");

        // Longitudes alineadas con el esquema de Customers (RA-869d7f32r): validar
        // aquí evita que el fallo aparezca como error de base de datos.
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
            .Must(category => CustomerCategories.All.Contains(category!))
                .WithMessage($"La categoría debe ser una de: {string.Join(", ", CustomerCategories.All)}.")
            .When(x => x.Category is not null);

        RuleFor(x => x.PreferredContactMethod)
            .NotEmpty().WithMessage("El canal de contacto es obligatorio.")
            .Must(method => CustomerContactMethods.All.Contains(method))
                .WithMessage($"El canal de contacto debe ser uno de: {string.Join(", ", CustomerContactMethods.All)}.");

        RuleFor(x => x.GrantedConsents)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Indica los consentimientos aceptados.")
            // Sin tratamiento de datos no hay alta: es la base legal para
            // gestionar las citas, y nunca se da por otorgado sin marcarlo.
            .Must(consents => CustomerConsentTypes.Required.All(consents.Contains))
                .WithMessage("El consentimiento de tratamiento de datos es obligatorio.")
            // El índice único de consentimientos vigentes rechazaría el duplicado
            // al guardar; aquí se devuelve como error de validación.
            .Must(consents => consents.Distinct().Count() == consents.Count)
                .WithMessage("Un consentimiento no puede repetirse.");

        RuleForEach(x => x.GrantedConsents)
            .Must(type => CustomerConsentTypes.All.Contains(type))
                .WithMessage($"Cada consentimiento debe ser uno de: {string.Join(", ", CustomerConsentTypes.All)}.")
            .When(x => x.GrantedConsents is not null);
    }
}
