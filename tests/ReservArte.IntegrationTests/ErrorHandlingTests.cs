using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReservArte.Application.Common;
using ReservArte.Application.DTOs.Appointments;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;
using ReservArte.IntegrationTests.Infrastructure;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// Errores que no pasan por un controlador (RA-869f74u70): una excepción no
/// controlada, el tenant sin resolver y el límite de peticiones. Todos salen con
/// envelope y con el status del mapa único. Las excepciones y el límite se prueban
/// en variantes de la API (<c>WithWebHostBuilder</c>) que comparten la base, pero
/// no los servicios: ni el servicio que falla ni el contador de logins afectan al
/// resto de tests.
/// </summary>
[Collection(ApiCollection.Name)]
public class ErrorHandlingTests(ApiFactory factory)
{
    private const string Secret = "detalle interno que no debe salir";

    [Fact]
    public async Task Una_excepcion_no_controlada_sale_como_500_GEN_INTERNAL_ERROR_con_envelope()
    {
        await using var throwing = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAvailabilityService>();
            services.AddScoped<IAvailabilityService, ThrowingAvailabilityService>();
        }));
        var admin = await factory.CreateEmployeeAsync(TestData.OrgA, Roles.Admin);
        var token = await factory.TokenForAsync(TestData.OrgA, admin.Id);

        var result = await throwing.SendAsync(HttpMethod.Get,
            "/api/v1/appointments/availability?employeeId=1&date=2030-01-07&durationMinutes=30",
            TestData.OrgA, token);

        result.Status.Should().Be(HttpStatusCode.InternalServerError);
        result.ShouldBeEnvelope(success: false);
        result.ErrorCode.Should().Be(ErrorCodes.GenInternalError);

        // La API corre en Development: el tipo y el mensaje ayudan a depurar, pero
        // la traza no sale nunca. Fuera de Development no sale nada (GlobalExceptionHandlerTests).
        var details = result.Body.GetProperty("error").GetProperty("details");
        details.GetProperty("exception").GetString().Should().Be(typeof(InvalidOperationException).FullName);
        details.GetProperty("message").GetString().Should().Be(Secret);
        result.Body.ToString().Should().NotContain("   at ");
    }

    [Theory]
    [InlineData("no-es-un-guid")]
    [InlineData("99999999-9999-9999-9999-999999999999")]
    public async Task Sin_organizacion_resoluble_da_400_ORG_TENANT_NOT_RESOLVED_con_envelope(string header)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/customers");
        request.Headers.Add("X-Organization-Id", header);

        using var response = await client.SendAsync(request);
        var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        body.GetProperty("error").GetProperty("code").GetString().Should().Be(ErrorCodes.OrgTenantNotResolved);
        body.GetProperty("meta").GetProperty("requestId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Pasado_el_limite_de_login_da_429_GEN_RATE_LIMITED_con_envelope_y_Retry_After()
    {
        // Variante propia: el contador de intentos vive en sus servicios, no en los del resto.
        await using var limited = factory.WithWebHostBuilder(_ => { });
        var wrong = new { email = TestData.AdminA.Email, password = "No-Es-La-Buena-1" };

        var statuses = new List<HttpStatusCode>();
        ApiResult last = null!;
        for (var i = 0; i < 11; i++)
        {
            last = await limited.SendAsync(HttpMethod.Post, "/api/v1/auth/login", TestData.OrgA, token: null, wrong);
            statuses.Add(last.Status);
        }

        statuses.Take(10).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
        last.Status.Should().Be(HttpStatusCode.TooManyRequests);
        last.ShouldBeEnvelope(success: false);
        last.ErrorCode.Should().Be(ErrorCodes.GenRateLimited);
        last.Headers.RetryAfter.Should().NotBeNull();
    }

    private sealed class ThrowingAvailabilityService : IAvailabilityService
    {
        public Task<Result<AvailabilityResponse>> GetAvailableSlotsAsync(
            int employeeId, DateOnly date, int durationMinutes, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(Secret);

        public Task<Result<bool>> EnsureSlotAvailableAsync(
            int employeeId, DateOnly date, TimeOnly startTime, TimeOnly endTime,
            int? excludeAppointmentId = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(Secret);
    }
}
