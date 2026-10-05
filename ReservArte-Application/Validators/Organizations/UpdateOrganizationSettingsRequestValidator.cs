using FluentValidation;
using ReservArte.Application.DTOs.Organizations;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Validators.Organizations;

/// <summary>
/// Validación de la configuración del centro (RA-869f74u7y). Los rangos son los
/// de los CHECK de <c>OrganizationSettings</c>.
/// </summary>
public class UpdateOrganizationSettingsRequestValidator : AbstractValidator<UpdateOrganizationSettingsRequest>
{
    public const string UnknownTimeZoneCode = "UnknownTimeZone";

    public UpdateOrganizationSettingsRequestValidator()
    {
        // Una zona que el servidor no resuelve dejaría al centro calculando
        // «hoy» y «ahora» en UTC sin que nadie lo viera: se rechaza al guardar.
        RuleFor(x => x.TimeZone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La zona horaria es obligatoria.")
            .MaximumLength(OrganizationSettings.TimeZoneMaxLength)
                .WithMessage($"La zona horaria no puede superar los {OrganizationSettings.TimeZoneMaxLength} caracteres.")
            .Must(BeIanaTimeZone)
                .WithErrorCode(UnknownTimeZoneCode)
                .WithMessage("La zona horaria debe ser un identificador IANA válido, como «Europe/Madrid» o «Atlantic/Canary».");

        RuleFor(x => x.CancellationHoursThreshold)
            .NotNull().WithMessage("El umbral de cancelación es obligatorio.")
            .InclusiveBetween(
                OrganizationSettings.MinCancellationHoursThreshold,
                OrganizationSettings.MaxCancellationHoursThreshold)
            .WithMessage(
                $"El umbral de cancelación debe estar entre {OrganizationSettings.MinCancellationHoursThreshold} y {OrganizationSettings.MaxCancellationHoursThreshold} horas.");

        RuleFor(x => x.MaxNoShowsBeforeBlock)
            .NotNull().WithMessage("El máximo de no presentaciones es obligatorio.")
            .InclusiveBetween(
                OrganizationSettings.MinMaxNoShowsBeforeBlock,
                OrganizationSettings.MaxMaxNoShowsBeforeBlock)
            .WithMessage(
                $"El máximo de no presentaciones debe estar entre {OrganizationSettings.MinMaxNoShowsBeforeBlock} y {OrganizationSettings.MaxMaxNoShowsBeforeBlock}.");
    }

    /// <summary>
    /// IANA y no de Windows: en Windows, .NET resuelve también «Romance Standard
    /// Time», que un servidor Linux sin ICU completo podría no entender.
    /// </summary>
    private static bool BeIanaTimeZone(string? id) =>
        id is not null
        && id == id.Trim()
        && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var timeZone)
        && timeZone.HasIanaId;
}
