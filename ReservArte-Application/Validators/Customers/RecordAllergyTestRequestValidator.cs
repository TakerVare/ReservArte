using FluentValidation;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Application.Validators.Employees;

namespace ReservArte.Application.Validators.Customers;

/// <summary>
/// La fecha es obligatoria y con zona (RA-869f8pmnm). Que no sea futura lo comprueba
/// el servicio, que tiene el reloj.
/// </summary>
public class RecordAllergyTestRequestValidator : AbstractValidator<RecordAllergyTestRequest>
{
    public RecordAllergyTestRequestValidator()
    {
        RuleFor(x => x.TestedAt)
            .NotEmpty().WithMessage("Indica cuándo se hizo la prueba.")
            .Must(value => value.Kind == DateTimeKind.Utc)
                .WithMessage(CreateEmployeeExceptionRequestValidator.MissingZoneMessage)
                .WithErrorCode(CreateEmployeeExceptionRequestValidator.MissingZoneCode);
    }
}
