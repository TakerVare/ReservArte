namespace ReservArte.Application.DTOs.Services;

/// <summary>
/// Tarifa de un servicio para un nivel de empleada.
///
/// El nivel viaja en la ruta, no en el cuerpo: es la clave natural de la tarifa
/// (solo hay una vigente por servicio y nivel, impuesto por el índice único
/// filtrado). Por eso la operación es un `PUT` idempotente que crea o actualiza,
/// y no un `POST` que pudiera chocar con la que ya existe.
/// </summary>
public class UpsertServicePricingRequest
{
    /// <summary>Precio final para ese nivel, no un recargo sobre el base.</summary>
    public decimal Price { get; init; }
}
