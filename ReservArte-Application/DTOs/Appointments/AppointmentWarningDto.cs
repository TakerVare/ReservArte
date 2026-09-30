namespace ReservArte.Application.DTOs.Appointments;

/// <summary>
/// Aviso sobre una cita que no impide reservarla, pero que el personal debe ver
/// (RA-869f9cu2x). Hoy, solo la prueba de alergia previa.
/// </summary>
public class AppointmentWarningDto
{
    /// <summary><see cref="AppointmentWarningCodes"/>.</summary>
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    /// <summary>Servicio de la cita al que se refiere el aviso.</summary>
    public int? ServiceId { get; init; }
}

/// <summary>Códigos de <see cref="AppointmentWarningDto"/>.</summary>
public static class AppointmentWarningCodes
{
    /// <summary>El servicio exige prueba de alergia y la clienta no tiene ninguna registrada.</summary>
    public const string AllergyTestMissing = "AllergyTestMissing";

    /// <summary>La última prueba no llega a las horas de antelación que exige el servicio.</summary>
    public const string AllergyTestTooLate = "AllergyTestTooLate";
}
