using FluentValidation;
using ReservArte.Application.DTOs.Appointments;

namespace ReservArte.Application.Validators.Appointments;

public class CancelAppointmentRequestValidator : AbstractValidator<CancelAppointmentRequest>
{
    public CancelAppointmentRequestValidator()
    {
        // Longitud alineada con Appointments.CancellationReason NVARCHAR(500).
        // A diferencia de la nota de cliente, el motivo es opcional: una clienta
        // puede cancelar sin dar explicaciones.
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("El motivo no puede superar los 500 caracteres.");
    }
}
