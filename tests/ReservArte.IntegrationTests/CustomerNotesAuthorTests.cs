using System.Net;
using AwesomeAssertions;
using ReservArte.Domain.Entities;
using ReservArte.IntegrationTests.Infrastructure;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Notas de la clienta con el nombre de su autora (4.2): la ficha la ve todo el
/// personal, y la lista de empleados solo Admin y Manager, así que el nombre viaja
/// con la nota.
/// </summary>
[Collection(ApiCollection.Name)]
public class CustomerNotesAuthorTests(ApiFactory factory)
{
    [Fact]
    public async Task La_nota_lleva_el_nombre_de_su_autora_al_crearla_y_en_la_ficha()
    {
        var author = await factory.CreateEmployeeAsync(TestData.OrgA);
        var customer = await factory.CreateCustomerAsync(TestData.OrgA);
        var token = await factory.TokenForAsync(TestData.OrgA, author.Id);
        var expected = $"{author.FirstName} {author.LastName}".Trim();

        var created = await factory.SendAsync(
            HttpMethod.Post, $"/api/v1/customers/{customer.Id}/notes", TestData.OrgA, token,
            new { note = "Prefiere cita por la tarde" });
        var profile = await factory.SendAsync(
            HttpMethod.Get, $"/api/v1/customers/{customer.Id}", TestData.OrgA, token);

        created.Status.Should().Be(HttpStatusCode.Created);
        created.ShouldBeEnvelope(success: true);
        created.Data.GetProperty("employeeName").GetString().Should().Be(expected);
        var note = profile.Data.GetProperty("notes").EnumerateArray().Single();
        note.GetProperty("employeeId").GetInt32().Should().Be(author.Id);
        note.GetProperty("employeeName").GetString().Should().Be(expected);
    }
}
