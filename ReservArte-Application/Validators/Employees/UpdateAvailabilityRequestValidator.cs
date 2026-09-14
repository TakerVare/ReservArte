using FluentValidation;
using ReservArte.Application.DTOs.Employees;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Validators.Employees;

public class UpdateAvailabilityRequestValidator : AbstractValidator<UpdateAvailabilityRequest>
{
    /// <summary>
    /// Tope de tramos por petición. Una semana real no pasa de unos pocos por
    /// día; el límite evita que un payload absurdo llegue a la base de datos.
    /// </summary>
    public const int MaxSlots = 50;

    public UpdateAvailabilityRequestValidator()
    {
        RuleFor(x => x.WeeklySchedule)
            .NotNull().WithMessage("El horario semanal es obligatorio (puede ir vacío para dejar al empleado sin horario).")
            .Must(slots => slots.Count <= MaxSlots)
                .WithMessage($"El horario no puede tener más de {MaxSlots} tramos.")
            .Must(NoOverlaps)
                .WithMessage("Hay tramos que se solapan en el mismo día.")
            .When(x => x.WeeklySchedule is not null);

        RuleForEach(x => x.WeeklySchedule).SetValidator(new AvailabilitySlotRequestValidator());
    }

    /// <summary>
    /// Varios tramos el mismo día son normales (jornada partida de mañana y
    /// tarde); lo que no puede haber es solape. Los tramos con fin anterior al
    /// inicio los rechaza antes el validador de tramo.
    /// </summary>
    private static bool NoOverlaps(IReadOnlyList<AvailabilitySlotRequest> slots)
    {
        foreach (var day in slots.GroupBy(s => s.DayOfWeek))
        {
            var ordered = day.OrderBy(s => s.StartTime).ToList();

            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

public class AvailabilitySlotRequestValidator : AbstractValidator<AvailabilitySlotRequest>
{
    public AvailabilitySlotRequestValidator()
    {
        // Espejo de CK_EmployeeAvailabilities_DayOfWeek: validarlo aquí lo
        // convierte en GEN_VALIDATION_FAILED y no en un error de base de datos.
        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(WeekDay.Monday, WeekDay.Sunday)
                .WithMessage("El día debe estar entre 0 (lunes) y 6 (domingo).");

        // La tabla de horarios NO tiene CHECK de intervalo (la de ausencias sí),
        // así que sin esta regla entraría un tramo imposible.
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
                .WithMessage("La hora de fin debe ser posterior a la de inicio.");
    }
}
