namespace ReservArte.Application.DTOs.Appointments;

/// <summary>
/// Motivo de la cancelación (RA-869d7f4xf). Quién cancela **no** viaja en el
/// cuerpo: lo deduce el servidor de la cuenta que llama, para que nadie pueda
/// atribuir su cancelación a otra persona.
/// </summary>
public class CancelAppointmentRequest
{
    /// <summary>
    /// Motivo, opcional y de 500 caracteres como mucho (lo que admite la
    /// columna). La clienta puede no dar ninguno.
    /// </summary>
    public string? Reason { get; init; }
}
