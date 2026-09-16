using FluentValidation;
using ReservArte.Application.DTOs.Services;

namespace ReservArte.Application.Validators.Services;

/// <summary>
/// Validación de la tarifa por nivel. El nivel viaja en la ruta, así que aquí
/// solo se valida el importe; que el nivel pertenezca a `EmployeeLevels` lo
/// comprueba `ServiceCatalogService`, antes de que salte el CHECK del esquema.
/// </summary>
public class UpsertServicePricingRequestValidator : AbstractValidator<UpsertServicePricingRequest>
{
    public UpsertServicePricingRequestValidator()
    {
        // Coincide con el CHECK `CK_ServicePricings_Price`. El 0 es legítimo:
        // un servicio puede ser gratuito para un nivel concreto.
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");
    }
}
