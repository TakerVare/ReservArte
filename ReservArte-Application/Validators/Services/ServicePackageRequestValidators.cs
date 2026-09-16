using FluentValidation;
using ReservArte.Application.DTOs.Services;

namespace ReservArte.Application.Validators.Services;

/// <summary>
/// Reglas comunes al alta y a la edición de paquetes. Se comparten para que
/// editar no pueda dejar un paquete en un estado que el alta rechazaría.
///
/// Lo que NO se comprueba aquí: que cada `serviceId` exista **en este centro**.
/// Eso exige mirar la base de datos y lo resuelve `ServicePackageService`.
/// </summary>
internal static class ServicePackageRules
{
    public static void Apply<T>(AbstractValidator<T> validator,
        Func<T, string> name,
        Func<T, string?> description,
        Func<T, string?> imageUrl,
        Func<T, decimal> totalPrice,
        Func<T, decimal> discountPercentage,
        Func<T, IReadOnlyList<ServicePackageItemRequest>> items)
    {
        // Longitudes alineadas con el esquema de ServicePackages (RA-869d7f3z0).
        validator.RuleFor(x => name(x))
            .NotEmpty().WithName("name").WithMessage("El nombre es obligatorio.")
            .MaximumLength(200).WithName("name")
                .WithMessage("El nombre no puede superar los 200 caracteres.");

        validator.RuleFor(x => description(x))
            .MaximumLength(1000).WithName("description")
                .WithMessage("La descripción no puede superar los 1000 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(description(x)));

        validator.RuleFor(x => imageUrl(x))
            .MaximumLength(500).WithName("imageUrl")
                .WithMessage("La URL de la imagen no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(imageUrl(x)));

        // Coincide con CK_ServicePackages_TotalPrice.
        validator.RuleFor(x => totalPrice(x))
            .GreaterThanOrEqualTo(0).WithName("totalPrice")
                .WithMessage("El precio del paquete no puede ser negativo.");

        // Coincide con CK_ServicePackages_DiscountPercentage: es un tanto por
        // ciento, fuera de 0-100 no significa nada.
        validator.RuleFor(x => discountPercentage(x))
            .InclusiveBetween(0, 100).WithName("discountPercentage")
                .WithMessage("El descuento debe estar entre 0 y 100.");

        validator.RuleFor(x => items(x))
            .Cascade(CascadeMode.Stop)
            .NotNull().WithName("items").WithMessage("Indica los servicios del paquete.")
            // Un paquete sin servicios no es un paquete.
            .Must(list => list.Count > 0).WithName("items")
                .WithMessage("El paquete debe incluir al menos un servicio.")
            // Repetir un servicio haría ambiguo su orden dentro del paquete.
            .Must(list => list.Select(i => i.ServiceId).Distinct().Count() == list.Count)
                .WithName("items")
                .WithMessage("Un servicio no puede repetirse en el paquete.");
    }
}

/// <summary>Validación de entrada del alta de paquete.</summary>
public class CreateServicePackageRequestValidator : AbstractValidator<CreateServicePackageRequest>
{
    public CreateServicePackageRequestValidator()
    {
        ServicePackageRules.Apply(
            this, x => x.Name, x => x.Description, x => x.ImageUrl,
            x => x.TotalPrice, x => x.DiscountPercentage, x => x.Items);

        RuleForEach(x => x.Items).SetValidator(new ServicePackageItemRequestValidator())
            .When(x => x.Items is not null);
    }
}

/// <summary>Validación de entrada de la edición de paquete; mismas reglas.</summary>
public class UpdateServicePackageRequestValidator : AbstractValidator<UpdateServicePackageRequest>
{
    public UpdateServicePackageRequestValidator()
    {
        ServicePackageRules.Apply(
            this, x => x.Name, x => x.Description, x => x.ImageUrl,
            x => x.TotalPrice, x => x.DiscountPercentage, x => x.Items);

        RuleForEach(x => x.Items).SetValidator(new ServicePackageItemRequestValidator())
            .When(x => x.Items is not null);
    }
}

/// <summary>Validación de cada línea del paquete.</summary>
public class ServicePackageItemRequestValidator : AbstractValidator<ServicePackageItemRequest>
{
    public ServicePackageItemRequestValidator()
    {
        RuleFor(x => x.ServiceId)
            .GreaterThan(0).WithMessage("El servicio indicado no es válido.");

        // Es una posición en la secuencia: un negativo no significa nada.
        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("El orden no puede ser negativo.");
    }
}
