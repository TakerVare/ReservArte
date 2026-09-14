using FluentValidation;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Validators.Employees;

public class CreateEmployeeExceptionRequestValidator : AbstractValidator<CreateEmployeeExceptionRequest>
{
    public CreateEmployeeExceptionRequestValidator()
    {
        RuleFor(x => x.StartDateTime)
            .NotEmpty().WithMessage("La fecha de inicio es obligatoria.");

        // Espejo de CK_EmployeeExceptions_Interval: un intervalo invertido no
        // es una ausencia, es un dato corrupto.
        RuleFor(x => x.EndDateTime)
            .NotEmpty().WithMessage("La fecha de fin es obligatoria.")
            .GreaterThan(x => x.StartDateTime)
                .WithMessage("La fecha de fin debe ser posterior a la de inicio.");

        // Espejo de CK_EmployeeExceptions_Type. Lista blanca: un tipo
        // desconocido debe fallar, no colarse.
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("El tipo de ausencia es obligatorio.")
            .Must(type => EmployeeExceptionTypes.All.Contains(type))
                .WithMessage($"El tipo debe ser uno de: {string.Join(", ", EmployeeExceptionTypes.All)}.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("El motivo no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}
