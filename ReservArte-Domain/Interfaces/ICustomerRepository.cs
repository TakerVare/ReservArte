using ReservArte.Domain.Common;
using ReservArte.Domain.Entities;

namespace ReservArte.Domain.Interfaces;

/// <summary>
/// Criterios de búsqueda de la lista de clientes. Todos opcionales: sin
/// ninguno, devuelve los clientes activos del tenant actual.
/// </summary>
public class CustomerFilter
{
    /// <summary>Busca en nombre, apellidos y email (contiene).</summary>
    public string? Search { get; init; }

    /// <summary>Valor de <see cref="CustomerCategories"/>. Null = cualquiera.</summary>
    public string? Category { get; init; }

    /// <summary>Null = bloqueados y no bloqueados.</summary>
    public bool? IsBlocked { get; init; }

    /// <summary>
    /// Null devuelve solo los activos: la baja es lógica y la lista de gestión
    /// no debe arrastrar bajas salvo que se pidan explícitamente.
    /// </summary>
    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Acceso a datos de clientes (RA-869d7f32r). Todas las operaciones quedan
/// acotadas al tenant actual; sin organización resuelta no devuelven nada.
///
/// El historial de citas del cliente llegará con el módulo de Citas: hoy
/// <see cref="Appointment"/> no está mapeado y no hay nada que consultar.
/// </summary>
public interface ICustomerRepository
{
    /// <summary>Lista paginada con filtros, ordenada por apellidos y nombre.</summary>
    Task<PagedResult<Customer>> GetPagedAsync(
        CustomerFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Ficha del cliente, o null si no existe en el tenant actual.</summary>
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perfil completo de solo lectura: la ficha con sus notas, alergias y
    /// consentimientos vigentes (los dados de baja no se cargan).
    /// </summary>
    Task<Customer?> GetProfileAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cliente con ese email en la organización actual, o null. El email es
    /// único dentro de la organización (RA-869f1xc0u), así que hay a lo sumo uno.
    /// </summary>
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    void Add(Customer customer);

    void Update(Customer customer);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
