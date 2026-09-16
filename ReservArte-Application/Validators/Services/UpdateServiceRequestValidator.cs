using FluentValidation;
using ReservArte.Application.DTOs.Services;

namespace ReservArte.Application.Validators.Services;

/// <summary>
/// Validación de entrada de la edición de servicio. Mismas reglas que el alta:
/// la edición no puede dejar el servicio en un estado que el alta rechazaría.
/// </summary>
public class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("La descripción no puede superar los 1000 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("La duración debe ser mayor que 0 minutos.");

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio base no puede ser negativo.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("La categoría indicada no es válida.")
            .When(x => x.CategoryId.HasValue);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("La URL de la imagen no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));

        RuleFor(x => x.AllergyTestHoursBefore)
            .GreaterThan(0).WithMessage("La antelación de la prueba de alergia debe ser mayor que 0 horas.")
            .When(x => x.RequiresAllergyTest);
    }
}
