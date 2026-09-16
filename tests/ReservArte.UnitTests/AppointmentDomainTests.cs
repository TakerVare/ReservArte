using FluentAssertions;
using ReservArte.Domain.Entities;
using Xunit;

namespace ReservArte.UnitTests;

/// <summary>
/// Contrato de dominio del módulo de Citas (RA-869d7f4f1).
///
/// Mismo criterio que <see cref="CustomerDomainTests"/> y
/// <see cref="ServiceDomainTests"/>: los catálogos se persisten como texto con
/// un CHECK en el esquema, y un valor que no coincida no falla al compilar.
/// Estos tests fijan los valores y el tipo del tenant antes de que exista la
/// migración.
/// </summary>
public class AppointmentDomainTests
{
    [Fact]
    public void Los_estados_son_los_ocho_del_CHECK_de_diseno()
    {
        // Vol. 1 §5.2.2: la cancelación se desdobla en tres valores a propósito.
        AppointmentStatuses.All.Should().BeEquivalentTo(new[]
        {
            "pending", "confirmed", "in_progress", "completed",
            "cancelled", "cancelled_by_customer", "cancelled_by_business", "no_show",
        });
    }

    [Fact]
    public void Las_cancelaciones_son_los_tres_valores_que_significan_cancelada()
    {
        // Existe para no repetir los tres literales en cada consulta que filtre
        // cancelaciones.
        AppointmentStatuses.Cancellations.Should().BeEquivalentTo(new[]
        {
            "cancelled", "cancelled_by_customer", "cancelled_by_business",
        });

        AppointmentStatuses.Cancellations.Should().BeSubsetOf(AppointmentStatuses.All);
    }

    [Fact]
    public void Los_estados_terminales_no_incluyen_ninguno_abierto()
    {
        // Desde un terminal no se vuelve a estados abiertos (§5.2.2): reagendar
        // es una cita nueva.
        AppointmentStatuses.Terminal.Should().BeSubsetOf(AppointmentStatuses.All);
        AppointmentStatuses.Terminal.Should().NotContain(AppointmentStatuses.Pending);
        AppointmentStatuses.Terminal.Should().NotContain(AppointmentStatuses.Confirmed);
        AppointmentStatuses.Terminal.Should().NotContain(AppointmentStatuses.InProgress);
        AppointmentStatuses.Terminal.Should().Contain(AppointmentStatuses.Cancellations);
    }

    [Fact]
    public void Los_tipos_de_cancelacion_son_cliente_y_centro()
    {
        AppointmentCancelledByTypes.All.Should().BeEquivalentTo(new[] { "customer", "business" });
    }

    [Fact]
    public void Todos_los_valores_de_catalogo_estan_en_snake_case_minusculas()
    {
        // Misma convención que CustomerCategories y EmployeeLevels. Roles es la
        // excepción (PascalCase) porque lo impone [Authorize(Roles = …)].
        var values = AppointmentStatuses.All.Concat(AppointmentCancelledByTypes.All);

        values.Should().AllSatisfy(v => v.Should().MatchRegex("^[a-z]+(_[a-z]+)*$"));
    }

    [Fact]
    public void Una_cita_nueva_nace_pendiente_y_activa()
    {
        var appointment = new Appointment();

        AppointmentStatuses.All.Should().Contain(appointment.Status);
        appointment.Status.Should().Be(AppointmentStatuses.Pending);
        appointment.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(typeof(Appointment))]
    [InlineData(typeof(AppointmentServiceItem))]
    [InlineData(typeof(WaitingList))]
    public void El_tenant_es_un_Guid_como_Organization_Id(Type entity)
    {
        // Las tres nacieron con OrganizationId int (o sin él), incompatible con
        // Organization.Id (Guid): la FK no se habría podido mapear. Mismo
        // arreglo que Customer (RA-869d7f2z5) y el catálogo (RA-869d7f3wa).
        entity.GetProperty("OrganizationId")!.PropertyType.Should().Be(typeof(Guid));
        entity.GetProperty("Organization").Should().NotBeNull("el query filter necesita la navegación");
    }

    [Fact]
    public void La_linea_de_cita_estrena_tenant_propio()
    {
        // Redundante con Appointment a propósito (RA-869f17myx): el query filter
        // global no puede depender de un JOIN.
        typeof(AppointmentServiceItem).GetProperty("OrganizationId").Should().NotBeNull();
    }

    [Theory]
    [InlineData("PaymentMethod")]
    [InlineData("PaymentMethodId")]
    [InlineData("Payments")]
    [InlineData("Photos")]
    [InlineData("ReminderLogs")]
    [InlineData("ConfirmationTokens")]
    public void La_cita_no_navega_a_modulos_que_aun_no_existen(string member)
    {
        // Redsys, fotografías y recordatorios siguen en Ignore. EF descarta las
        // navegaciones hacia tipos ignorados, así que dejarlas no rompería el
        // modelo; se retiran por el mismo criterio que en Customer y Service.
        typeof(Appointment).GetProperty(member).Should().BeNull();
    }

    [Fact]
    public void La_cita_conserva_los_escalares_de_Redsys_que_no_dependen_de_su_modulo()
    {
        // No son FK: son el número de pedido y el token de pre-autorización, y
        // RA-869d7f4j8 pondrá índice único sobre el primero.
        typeof(Appointment).GetProperty("RedsysOrderNumber").Should().NotBeNull();
        typeof(Appointment).GetProperty("RedsysPreAuthToken").Should().NotBeNull();
    }

    [Fact]
    public void La_lista_de_espera_nace_activa_y_sin_aviso()
    {
        var entry = new WaitingList();

        entry.IsActive.Should().BeTrue();
        entry.NotifiedAt.Should().BeNull();
        // Hueco por encima y por debajo sin renumerar la lista.
        entry.Priority.Should().Be(1000);
    }
}
