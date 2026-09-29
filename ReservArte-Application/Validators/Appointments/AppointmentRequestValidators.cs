using FluentValidation;
using ReservArte.Application.DTOs.Appointments;

namespace ReservArte.Application.Validators.Appointments;

/// <summary>
/// Validación de entrada del alta y la edición de citas (RA-869d7f519). Las dos
/// comparten reglas para que editar no pueda dejar una cita que el alta rechazaría.
///
/// Lo que NO se comprueba aquí, porque exige mirar la base de datos o el horario:
/// que la clienta, la empleada y los servicios existan en este centro, que la
/// empleada preste esos servicios, que la cita quepa en el día y que el hueco
/// esté libre. Eso lo resuelve <c>AppointmentService</c>.
/// </summary>
public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Indica la clienta.");

        AppointmentRules.Apply(this, x => x.EmployeeId, x => x.AppointmentDate, x => x.Items, x => x.Notes);
        RuleForEach(x => x.Items).SetValidator(new AppointmentItemRequestValidator());
    }
}

public class UpdateAppointmentRequestValidator : AbstractValidator<UpdateAppointmentRequest>
{
    public UpdateAppointmentRequestValidator()
    {
        AppointmentRules.Apply(this, x => x.EmployeeId, x => x.AppointmentDate, x => x.Items, x => x.Notes);
        RuleForEach(x => x.Items).SetValidator(new AppointmentItemRequestValidator());
    }
}

public class AppointmentItemRequestValidator : AbstractValidator<AppointmentItemRequest>
{
    public AppointmentItemRequestValidator()
    {
        RuleFor(x => x.ServiceId)
            .GreaterThan(0).WithMessage("Indica el servicio.");

        RuleFor(x => x.ServiceVariationId)
            .GreaterThan(0).WithMessage("La variación no es válida.")
            .When(x => x.ServiceVariationId is not null);
    }
}

internal static class AppointmentRules
{
    /// <summary>Tope de servicios por cita: más es casi seguro un error de la pantalla.</summary>
    public const int MaxItems = 10;

    public static void Apply<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, int>> employeeId,
        System.Linq.Expressions.Expression<Func<T, DateOnly>> appointmentDate,
        System.Linq.Expressions.Expression<Func<T, IReadOnlyList<AppointmentItemRequest>>> items,
        System.Linq.Expressions.Expression<Func<T, string?>> notes)
    {
        validator.RuleFor(employeeId)
            .GreaterThan(0).WithMessage("Indica la empleada.");

        validator.RuleFor(appointmentDate)
            .NotEqual(default(DateOnly)).WithMessage("Indica la fecha de la cita.");

        validator.RuleFor(items)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Indica los servicios de la cita.")
            .Must(list => list.Count > 0).WithMessage("La cita debe incluir al menos un servicio.")
            .Must(list => list.Count <= MaxItems)
                .WithMessage($"Una cita no puede tener más de {MaxItems} servicios.")
            // Repetir un servicio haría ambiguo su orden y su precio dentro de la cita.
            .Must(list => list.Select(i => i.ServiceId).Distinct().Count() == list.Count)
                .WithMessage("Un servicio no puede repetirse en la cita.");

        // Coincide con la columna Appointments.Notes (2000).
        validator.RuleFor(notes)
            .MaximumLength(2000).WithMessage("Las notas no pueden superar los 2000 caracteres.");
    }
}
