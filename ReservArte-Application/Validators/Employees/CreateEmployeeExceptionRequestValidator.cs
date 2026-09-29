using FluentValidation;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Validators.Employees;

public class CreateEmployeeExceptionRequestValidator : AbstractValidator<CreateEmployeeExceptionRequest>
{
    // RA-869f8pmnm: una fecha sin zona es ambigua (y PostgreSQL la rechaza). El conversor JSON deja
    // en UTC las que llegan con «Z» o con desplazamiento, y marca como Unspecified las que no.
    public const string MissingZoneMessage =
        "La fecha debe incluir la zona horaria (por ejemplo, 2026-11-02T08:00:00Z o 2026-11-02T09:00:00+01:00).";

    /// <summary>Mismo código que el rechazo de `from`/`to` sin zona en la disponibilidad.</summary>
    public const string MissingZoneCode = "MissingTimeZone";

    private static bool IsUtc(DateTime value) => value.Kind == DateTimeKind.Utc;

    public CreateEmployeeExceptionRequestValidator()
    {
        RuleFor(x => x.StartDateTime)
            .NotEmpty().WithMessage("La fecha de inicio es obligatoria.")
            .Must(IsUtc).WithMessage(MissingZoneMessage).WithErrorCode(MissingZoneCode);

        // Espejo de CK_EmployeeExceptions_Interval: un intervalo invertido no
        // es una ausencia, es un dato corrupto.
        RuleFor(x => x.EndDateTime)
            .NotEmpty().WithMessage("La fecha de fin es obligatoria.")
            .Must(IsUtc).WithMessage(MissingZoneMessage).WithErrorCode(MissingZoneCode)
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
