using FluentValidation;
using ReservArte.Application.DTOs.Auth;

namespace ReservArte.Application.Validators.Auth;

public class MfaVerifyRequestValidator : AbstractValidator<MfaVerifyRequest>
{
    public MfaVerifyRequestValidator()
    {
        RuleFor(x => x.MfaTicket)
            .NotEmpty().WithMessage("El ticket de verificación es obligatorio.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("El código es obligatorio.");
    }
}