using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ReservArte.Application.Interfaces;
using ReservArte.Domain.Entities;

namespace ReservArte.IntegrationTests.Infrastructure;

/// <summary>Respuesta HTTP leída: código, cabeceras y envelope <c>{ success, data, error, meta }</c>.</summary>
public sealed record ApiResult(HttpStatusCode Status, JsonElement Body, HttpResponseHeaders Headers)
{
    /// <summary>
    /// Comprueba que el cuerpo es el envelope completo y coherente con el código:
    /// éxito con <c>data</c> y sin <c>error</c>, fallo con <c>error.code</c> y
    /// <c>error.message</c>; <c>meta</c> con su <c>requestId</c> siempre.
    /// </summary>
    public void ShouldBeEnvelope(bool success)
    {
        Body.ValueKind.Should().Be(JsonValueKind.Object, $"HTTP {(int)Status} debe llevar envelope");
        Body.GetProperty("success").GetBoolean().Should().Be(success);
        Body.GetProperty("meta").GetProperty("requestId").GetString().Should().NotBeNullOrEmpty();

        if (success)
        {
            Body.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
        }
        else
        {
            var error = Body.GetProperty("error");
            error.GetProperty("code").GetString().Should().MatchRegex("^[A-Z]+(_[A-Z]+)+$");
            error.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
        }
    }

    public JsonElement Data => Body.GetProperty("data");

    /// <summary>Código de error del envelope, o null si la respuesta no trae error.</summary>
    public string? ErrorCode =>
        Body.ValueKind == JsonValueKind.Object
        && Body.TryGetProperty("error", out var error)
        && error.ValueKind == JsonValueKind.Object
            ? error.GetProperty("code").GetString()
            : null;
}

/// <summary>
/// Cliente de la API en memoria. El login está limitado a 10 por hora y, en el
/// TestServer, todas las peticiones comparten «IP», así que los tokens se piden
/// una vez por cuenta y se reutilizan.
/// </summary>
public static class ApiClient
{
    public static async Task<string> LoginAsync(this ApiFactory factory, TestAccount account, Guid organizationId)
    {
        var key = $"{organizationId}|{account.Email}";
        if (factory.Tokens.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = await factory.SendAsync(
            HttpMethod.Post, "/api/v1/auth/login", organizationId, token: null,
            new { email = account.Email, password = account.Password });

        if (result.Status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Login de {account.Email} → {(int)result.Status}: {result.Body}");
        }

        var token = result.Data.GetProperty("accessToken").GetString()!;
        factory.Tokens[key] = token;
        return token;
    }

    /// <summary>
    /// Token de acceso emitido por el propio <see cref="IJwtTokenService"/> de la
    /// API para una cuenta existente, sin pasar por el login (limitado a 10 por
    /// hora). La validación en el pipeline es la real: firma, emisor, rol y tenant.
    /// </summary>
    public static Task<string> TokenForAsync(this ApiFactory factory, Guid organizationId, int userId) =>
        factory.IssueAsync(organizationId, userId, (jwt, user) => jwt.GenerateAccessToken(user, organizationId));

    /// <summary>Ticket intermedio de 2FA (<c>mfa_pending</c>): válido en firma, no autoriza nada.</summary>
    public static Task<string> MfaTicketForAsync(this ApiFactory factory, Guid organizationId, int userId) =>
        factory.IssueAsync(organizationId, userId, (jwt, user) => jwt.GenerateMfaTicket(user, organizationId));

    private static async Task<string> IssueAsync(
        this ApiFactory factory, Guid organizationId, int userId, Func<IJwtTokenService, User, string> issue)
    {
        await using var scope = factory.CreateTenantScope(organizationId);
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"No existe el usuario {userId}.");
        return issue(scope.ServiceProvider.GetRequiredService<IJwtTokenService>(), user);
    }

    public static async Task<ApiResult> SendAsync(
        this WebApplicationFactory<Program> factory,
        HttpMethod method,
        string path,
        Guid? organizationId,
        string? token,
        object? body = null)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(method, path);

        if (organizationId is { } org)
        {
            request.Headers.Add("X-Organization-Id", org.ToString());
        }

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();
        var json = string.IsNullOrWhiteSpace(raw) ? default : JsonDocument.Parse(raw).RootElement.Clone();
        return new ApiResult(response.StatusCode, json, response.Headers);
    }
}
