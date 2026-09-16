using FluentValidation;
using ReservArte.Application.DTOs.Services;

namespace ReservArte.Application.Validators.Services;

/// <summary>
/// Validación de entrada del alta de categoría. Longitudes alineadas con el
/// esquema de `ServiceCategories` (RA-869d7f3z0): validar aquí evita que el
/// fallo aparezca como error de base de datos.
/// </summary>
public class CreateServiceCategoryRequestValidator
    : AbstractValidator<CreateServiceCategoryRequest>
{
    public CreateServiceCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Color)
            .MaximumLength(20).WithMessage("El color no puede superar los 20 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Color));

        // Es una posición en el listado: un negativo no significa nada.
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("El orden de presentación no puede ser negativo.");
    }
}

/// <summary>
/// Validación de la edición de categoría. Mismas reglas que el alta: editar no
/// puede dejar la categoría en un estado que el alta rechazaría.
/// </summary>
public class UpdateServiceCategoryRequestValidator
    : AbstractValidator<UpdateServiceCategoryRequest>
{
    public UpdateServiceCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Color)
            .MaximumLength(20).WithMessage("El color no puede superar los 20 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Color));

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("El orden de presentación no puede ser negativo.");
    }
}
