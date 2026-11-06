using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Offside.AspNetCore.Tests;

public sealed class SuccessEnvelopeTests
{
    private static readonly Notice Created = Notice.Create("order.created", new { id = 42 });

    [Theory]
    [InlineData(false, false, false, 204)]
    [InlineData(false, false, true, 204)]
    [InlineData(false, true, false, 200)]
    [InlineData(false, true, true, 200)]
    [InlineData(true, false, false, 204)]
    [InlineData(true, false, true, 200)]
    [InlineData(true, true, false, 200)]
    [InlineData(true, true, true, 200)]
    public async Task Preserves_success_contract_unless_envelope_is_enabled(
        bool enabled, bool hasData, bool hasNotice, int status)
    {
        using var services = Services(enabled);
        var http = Context(services);
        var result = hasData
            ? (hasNotice ? Result<int>.Success(42, Created) : Result<int>.Success(42)).ToHttpResult(http)
            : (hasNotice ? Result.Success(Created) : Result.Success()).ToHttpResult(http);
        await result.ExecuteAsync(http);
        Assert.Equal(status, http.Response.StatusCode);

        var text = Body(http);
        if (status == 204)
        {
            Assert.Empty(text);
            return;
        }

        if (!enabled)
        {
            Assert.Equal("42", text);
            return;
        }

        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.Equal(hasData, root.TryGetProperty("data", out var data));
        if (hasData)
            Assert.Equal(42, data.GetInt32());

        Assert.Equal(hasNotice, root.TryGetProperty("message", out var message));
        Assert.Equal(hasNotice, root.TryGetProperty("code", out var code));
        if (hasNotice)
        {
            Assert.Equal("Pedido 42 criado.", message.GetString());
            Assert.Equal(Created.Code, code.GetString());
        }

        Assert.Equal("test-trace", root.GetProperty("traceId").GetString());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Last_metadata_overrides_global_policy(bool global, bool endpoint)
    {
        using var services = Services(global);
        var http = Context(services);
        http.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(
            new OffsideSuccessEnvelopeAttribute(!endpoint), new OffsideSuccessEnvelopeAttribute(endpoint)), null));
        await Result.Success(Created).ToHttpResult(http).ExecuteAsync(http);
        Assert.Equal(endpoint ? 200 : 204, http.Response.StatusCode);
    }

    [Fact]
    public async Task Mvc_envelope_keeps_null_data_and_protects_reserved_fields()
    {
        using var services = Services(true);
        var http = Context(services);
        var options = services.GetRequiredService<OffsideAspNetCoreOptions>();
        options.CustomizeSuccess = (envelope, _, _) =>
        {
            envelope.Remove("data");
            envelope["CODE"] = "replacement";
            envelope["extra"] = 123;
        };
        var resolver = services.GetRequiredService<IErrorMessageResolver>();
        await Result<string?>.Success(null, Created).ToActionResult(resolver, null, options)
            .ExecuteResultAsync(new ActionContext(http, new RouteData(), new ActionDescriptor()));

        using var json = JsonDocument.Parse(Body(http));
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("data").ValueKind);
        Assert.Equal(Created.Code, json.RootElement.GetProperty("code").GetString());
        Assert.Equal(123, json.RootElement.GetProperty("extra").GetInt32());
    }

    [Fact]
    public async Task Callback_receives_message_with_envelope_disabled_and_only_runs_for_success()
    {
        using var services = Services(false);
        var http = Context(services);
        await Result<int>.Success(42, Created)
            .ToHttpResult(http, (value, message) => Results.Created($"/orders/{value}", message))
            .ExecuteAsync(http);

        Assert.Equal(201, http.Response.StatusCode);
        Assert.Equal("/orders/42", http.Response.Headers.Location.ToString());
        Assert.Equal("Pedido 42 criado.", JsonSerializer.Deserialize<string>(Body(http)));

        http = Context(services);
        await Result<int>.Failure(Error.NotFound("order", 42))
            .ToHttpResult(http, (_, _) => throw new InvalidOperationException())
            .ExecuteAsync(http);
        Assert.Equal(404, http.Response.StatusCode);
    }

    private static ServiceProvider Services(bool enabled)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOffside(options => options
            .AddJson(CultureInfo.InvariantCulture, "{}")
            .AddJson(CultureInfo.GetCultureInfo("pt"), """{"order.created":"Pedido {id} criado."}"""));
        services.AddOffsideAspNetCore(options =>
        {
            options.SuccessEnvelope = enabled;
            options.ResolveTraceId = _ => "test-trace";
        });
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext Context(IServiceProvider services)
    {
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Headers.AcceptLanguage = "pt-BR";
        http.Response.Body = new MemoryStream();
        return http;
    }

    private static string Body(DefaultHttpContext http) =>
        Encoding.UTF8.GetString(((MemoryStream)http.Response.Body).ToArray());
}
