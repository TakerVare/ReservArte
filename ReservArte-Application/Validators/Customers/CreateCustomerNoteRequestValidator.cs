using FluentValidation;
using ReservArte.Application.DTOs.Customers;

namespace ReservArte.Application.Validators.Customers;

public class CreateCustomerNoteRequestValidator : AbstractValidator<CreateCustomerNoteRequest>
{
    public CreateCustomerNoteRequestValidator()
    {
        // Longitud alineada con CustomerNotes.Note NVARCHAR(2000). NotEmpty
        // rechaza también una nota solo con espacios.
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("La nota no puede estar vacía.")
            .MaximumLength(2000).WithMessage("La nota no puede superar los 2000 caracteres.");
    }
}
