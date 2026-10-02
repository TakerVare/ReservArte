using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Customers;
using ReservArte.Domain.Common;
using ReservArte.Domain.Interfaces;

namespace ReservArte.Application.Interfaces;

/// <summary>
/// Casos de uso del módulo de Clientes (RA-869d7f369). Todas las operaciones
/// quedan acotadas al tenant de la petición; el servicio nunca lo acepta como
/// parámetro.
///
/// Una empleada puede ser clienta de su centro con la misma cuenta (decisión de
/// producto 2026-09-14): la ficha de cliente no exige `Rol = Customer`, y dar de
/// alta como cliente a quien ya tiene cuenta en el centro le añade la ficha en
/// lugar de crear otra cuenta.
/// </summary>
public interface ICustomerService
{
    /// <summary>Lista paginada con búsqueda y filtros.</summary>
    Task<Result<PagedResult<CustomerDto>>> GetPagedAsync(
        CustomerFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Perfil completo: ficha con consentimientos, alergias y notas vigentes.</summary>
    Task<Result<CustomerDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alta con los consentimientos aceptados (GEN_VALIDATION_FAILED sin
    /// `data_processing`). Nace con categoría `new` salvo que se indique otra.
    /// Si el email ya es de una cuenta del centro sin ficha de cliente, añade la
    /// ficha a esa cuenta sin tocarla; si ya hay ficha con ese email o esa
    /// cuenta ya la tiene, GEN_CONFLICT. Una cuenta nueva recibe la invitación
    /// para crear su contraseña.
    /// </summary>
    Task<Result<CustomerDetailDto>> CreateAsync(
        CreateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edita la ficha. Si la cuenta es solo de cliente, nombre, email, teléfono
    /// e imagen se sincronizan con ella. Si es de personal, la cuenta no se toca
    /// y cambiar el email es GEN_FORBIDDEN: se cambia desde Empleados.
    /// </summary>
    Task<Result<CustomerDto>> UpdateAsync(
        int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Baja lógica de la ficha. No bloquea la cuenta: solo la baja de empleado
    /// cierra el acceso (RA-869f180e5).
    /// </summary>
    Task<Result<CustomerDto>> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Reactiva una ficha dada de baja.</summary>
    Task<Result<CustomerDto>> ReactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra la última prueba de alergia de la clienta (RA-869f9cu2x). Todo el
    /// personal: la hace quien atiende. Fecha futura → 400 <c>testedAt</c>.
    /// </summary>
    Task<Result<CustomerDto>> RecordAllergyTestAsync(
        int id, RecordAllergyTestRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Añade una nota interna (RA-869d7f3fw). La autora es la ficha de empleado
    /// activa de quien llama; sin ella, GEN_FORBIDDEN.
    /// </summary>
    Task<Result<CustomerNoteDto>> AddNoteAsync(
        int customerId, CreateCustomerNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retira una nota (baja lógica, idempotente). Solo su autora, un Admin o un
    /// Manager; el resto, GEN_FORBIDDEN.
    /// </summary>
    Task<Result<CustomerNoteDto>> DeleteNoteAsync(
        int customerId, int noteId, CancellationToken cancellationToken = default);

    // ── Ficha completa (4.2b) ─────────────────────────────────────────────

    /// <summary>
    /// Da o retira un consentimiento y devuelve el perfil. Retirar `data_processing`
    /// da de baja la ficha (H-47). Tipo desconocido → 400 `consentType`.
    /// </summary>
    Task<Result<CustomerDetailDto>> SetConsentAsync(
        int customerId, string consentType, UpdateConsentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerAllergyDto>> AddAllergyAsync(
        int customerId, CustomerAllergyRequest request, CancellationToken cancellationToken = default);

    Task<Result<CustomerAllergyDto>> UpdateAllergyAsync(
        int customerId, int allergyId, CustomerAllergyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Baja lógica de una alergia; idempotente.</summary>
    Task<Result<CustomerAllergyDto>> DeleteAllergyAsync(
        int customerId, int allergyId, CancellationToken cancellationToken = default);

    /// <summary>Bloquea a la clienta con su motivo: no podrá reservar (`CUST_BLOCKED`).</summary>
    Task<Result<CustomerDto>> BlockAsync(
        int customerId, BlockCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>Quita el bloqueo y su motivo. Idempotente.</summary>
    Task<Result<CustomerDto>> UnblockAsync(int customerId, CancellationToken cancellationToken = default);
}
