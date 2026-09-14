using ReservArte.Domain.Entities;

namespace ReservArte.Application.DTOs.Employees;

/// <summary>
/// Alta de una ausencia puntual (POST .../exceptions). Las fechas se manejan
/// en UTC, como el resto de marcas de tiempo de la API.
/// </summary>
public class CreateEmployeeExceptionRequest
{
    public DateTime StartDateTime { get; init; }

    public DateTime EndDateTime { get; init; }

    /// <summary>Uno de `EmployeeExceptionTypes`. Por defecto, el genérico.</summary>
    public string Type { get; init; } = EmployeeExceptionTypes.Other;

    public string? Reason { get; init; }
}
