namespace ReservArte.Application.DTOs.Customers;

/// <summary>
/// Nota interna sobre un cliente. Ni el autor ni el cliente van en el cuerpo:
/// el cliente viene de la ruta y el autor es la ficha de empleado de quien
/// llama, para que nadie firme notas en nombre de otra persona.
/// </summary>
public class CreateCustomerNoteRequest
{
    public string Note { get; init; } = string.Empty;
}
