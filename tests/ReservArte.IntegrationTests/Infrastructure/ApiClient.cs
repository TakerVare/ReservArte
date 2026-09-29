using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ReservArte.IntegrationTests.Infrastructure;

/// <summary>Respuesta HTTP leída: código y envelope <c>{ success, data, error, meta }</c>.</summary>
public sealed record ApiResult(HttpStatusCode Status, JsonElement Body)
{
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

    public static async Task<ApiResult> SendAsync(
        this ApiFactory factory,
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
        return new ApiResult(response.StatusCode, json);
    }
}
