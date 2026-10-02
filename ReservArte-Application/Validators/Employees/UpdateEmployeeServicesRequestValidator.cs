using FluentValidation;
using ReservArte.Application.DTOs.Employees;

namespace ReservArte.Application.Validators.Employees;

public class UpdateEmployeeServicesRequestValidator : AbstractValidator<UpdateEmployeeServicesRequest>
{
    /// <summary>Tope por petición: un catálogo real no se acerca; frena payloads absurdos.</summary>
    public const int MaxServices = 200;

    public UpdateEmployeeServicesRequestValidator()
    {
        RuleFor(x => x.ServiceIds)
            .NotNull().WithMessage("La lista de servicios es obligatoria (puede ir vacía).")
            .Must(ids => ids.Count <= MaxServices)
                .WithMessage($"No se pueden asignar más de {MaxServices} servicios.")
            .When(x => x.ServiceIds is not null);

        RuleForEach(x => x.ServiceIds)
            .GreaterThan(0).WithMessage("El id de servicio debe ser positivo.");
    }
}
