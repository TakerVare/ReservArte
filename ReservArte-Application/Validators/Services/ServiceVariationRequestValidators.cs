using FluentValidation;
using ReservArte.Application.DTOs.Services;

namespace ReservArte.Application.Validators.Services;

/// <summary>
/// Validación de entrada del alta de variación.
///
/// Los modificadores pueden ser negativos a propósito (una variante más corta o
/// más barata). Lo que no puede quedar negativa o a cero es la duración
/// RESULTANTE, y eso no se puede comprobar aquí: depende del servicio al que se
/// añade. Lo valida `ServiceCatalogService`.
/// </summary>
public class CreateServiceVariationRequestValidator
    : AbstractValidator<CreateServiceVariationRequest>
{
    public CreateServiceVariationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");
    }
}

/// <summary>Validación de la edición de variación; mismas reglas que el alta.</summary>
public class UpdateServiceVariationRequestValidator
    : AbstractValidator<UpdateServiceVariationRequest>
{
    public UpdateServiceVariationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");
    }
}
