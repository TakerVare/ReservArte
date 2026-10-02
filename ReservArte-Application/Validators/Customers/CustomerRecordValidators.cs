using FluentValidation;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Validators.Customers;

public class CustomerAllergyRequestValidator : AbstractValidator<CustomerAllergyRequest>
{
    public CustomerAllergyRequestValidator()
    {
        // Longitudes del esquema (CustomerAllergies).
        RuleFor(x => x.AllergyDescription)
            .NotEmpty().WithMessage("Describe la alergia.")
            .MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.");

        RuleFor(x => x.Severity)
            .NotEmpty().WithMessage("La gravedad es obligatoria.")
            .Must(severity => AllergySeverities.All.Contains(severity))
                .WithMessage($"La gravedad debe ser una de: {string.Join(", ", AllergySeverities.All)}.");
    }
}

public class BlockCustomerRequestValidator : AbstractValidator<BlockCustomerRequest>
{
    public BlockCustomerRequestValidator()
    {
        // Un bloqueo sin motivo no se puede revisar después.
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Indica el motivo del bloqueo.")
            .MaximumLength(500).WithMessage("El motivo no puede superar los 500 caracteres.");
    }
}
