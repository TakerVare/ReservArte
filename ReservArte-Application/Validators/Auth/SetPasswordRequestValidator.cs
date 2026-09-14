using FluentValidation;
using ReservArte.Application.DTOs.Auth;

namespace ReservArte.Application.Validators.Auth;

/// <summary>
/// Mismas reglas que el restablecimiento: la política de contraseña es una sola
/// para todo el producto, y tener dos juegos divergentes acabaría admitiendo
/// por invitación lo que el reset rechaza.
/// </summary>
public class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
{
    public SetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("El token de invitación es obligatorio.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un dígito.")
            .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe contener al menos un símbolo.");
    }
}
