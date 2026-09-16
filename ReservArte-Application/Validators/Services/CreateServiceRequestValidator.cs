using FluentValidation;
using ReservArte.Application.DTOs.Services;

namespace ReservArte.Application.Validators.Services;

/// <summary>
/// Validación de entrada del alta de servicio. La existencia de la categoría no
/// se comprueba aquí sino en el servicio: exige mirar la base de datos y,
/// además, que sea del mismo centro.
/// </summary>
public class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceRequestValidator()
    {
        // Longitudes alineadas con el esquema de Services (RA-869d7f3z0): validar
        // aquí evita que el fallo aparezca como error de base de datos.
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("La descripción no puede superar los 1000 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        // Coincide con el CHECK del esquema: una duración de 0 o negativa haría
        // que el servicio no ocupase hueco en la agenda.
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

        // Solo importa si el servicio la exige: un 0 con la prueba activada
        // dejaría pasar una prueba de alergia el mismo día.
        RuleFor(x => x.AllergyTestHoursBefore)
            .GreaterThan(0).WithMessage("La antelación de la prueba de alergia debe ser mayor que 0 horas.")
            .When(x => x.RequiresAllergyTest);
    }
}
