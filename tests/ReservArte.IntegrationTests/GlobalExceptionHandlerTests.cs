using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using ReservArte.API.Middleware;
using ReservArte.Shared.Api;

namespace ReservArte.IntegrationTests;

/// <summary>
/// <see cref="GlobalExceptionHandler"/> fuera del pipeline, para los casos que la API
/// de tests (en Development) no puede provocar: otro entorno, un cliente que corta
/// la petición y una respuesta que ya había empezado a salir.
/// </summary>
public class GlobalExceptionHandlerTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Fuera_de_Development_la_respuesta_no_lleva_ningun_detalle_de_la_excepcion(string environment)
    {
        var context = NewContext();

        var handled = await Handler(environment).TryHandleAsync(
            context, new InvalidOperationException("cadena de conexión secreta"), CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(500);
        var body = ReadBody(context);
        body.GetProperty("error").GetProperty("code").GetString().Should().Be(ErrorCodes.GenInternalError);
        body.GetProperty("error").GetProperty("details").ValueKind.Should().Be(JsonValueKind.Null);
        body.ToString().Should().NotContain("secreta").And.NotContain(nameof(InvalidOperationException));
        body.GetProperty("meta").GetProperty("requestId").GetString().Should().Be(context.TraceIdentifier);
    }

    [Fact]
    public async Task Si_el_cliente_corta_la_peticion_no_se_responde_como_500()
    {
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        var context = NewContext();
        context.RequestAborted = aborted.Token;

        var handled = await Handler("Production").TryHandleAsync(
            context, new OperationCanceledException(aborted.Token), CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(499);
        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task Si_la_respuesta_ya_empezo_no_intenta_reescribirla()
    {
        var context = NewContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var handled = await Handler("Production").TryHandleAsync(
            context, new InvalidOperationException("tarde"), CancellationToken.None);

        handled.Should().BeFalse();
    }

    private static GlobalExceptionHandler Handler(string environment) =>
        new(NullLogger<GlobalExceptionHandler>.Instance, new FakeEnvironment(environment));

    private static DefaultHttpContext NewContext()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "req-123" };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static JsonElement ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body).RootElement.Clone();
    }

    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "ReservArte-API";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }
}
