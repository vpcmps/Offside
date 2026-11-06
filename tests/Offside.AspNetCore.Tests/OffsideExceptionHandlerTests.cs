using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Offside.AspNetCore.Tests;

public sealed class OffsideExceptionHandlerTests
{
    [Fact]
    public async Task Domain_exception_uses_same_response_as_result()
    {
        var error = Error.NotFound("order", 42);
        await using var app = await Start(new OffsideAspNetCoreOptions(), a =>
        {
            a.MapGet("/throw", (HttpContext _) => Throw(error.ToException()));
            a.MapGet("/result", (HttpContext http) => Result.Failure(error).ToHttpResult(http));
        });
        using var client = app.GetTestClient();
        using var thrown = await client.GetAsync("/throw");
        using var returned = await client.GetAsync("/result");
        Assert.Equal(404, (int)thrown.StatusCode);
        Assert.Equal(returned.Content.Headers.ContentType, thrown.Content.Headers.ContentType);
        Assert.Equal(await returned.Content.ReadAsStringAsync(), await thrown.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Multiple_errors_use_the_same_primary_as_a_failed_result()
    {
        Error[] errors = [Error.Validation("order"), Error.ServiceUnavailable("inventory")];
        await using var app = await Start(new OffsideAspNetCoreOptions(), a =>
            a.MapGet("/", (HttpContext _) => Throw(new DomainException(errors))));
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(503, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unexpected_exceptions_expose_details_only_in_debug(bool expose)
    {
        var log = new ExceptionLog();
        var options = new OffsideAspNetCoreOptions { ExposeExceptionDetails = expose };
        await using var app = await Start(options, a =>
        {
            a.MapGet("/throw", (HttpContext _) => Throw(new InvalidOperationException("private detail")));
            a.MapGet("/domain", (HttpContext _) => Throw(Error.NotFound("order", 42).ToException()));
        }, logging: log);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/throw");
        using var payload = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(500, (int)response.StatusCode);
        Assert.DoesNotContain("private detail", payload.RootElement.GetProperty("detail").GetString()!);
        Assert.Equal(expose, payload.RootElement.TryGetProperty("debug", out var debug) && debug.ValueKind != System.Text.Json.JsonValueKind.Null);
        if (expose)
            Assert.Contains(nameof(Throw), debug.GetString()!);

        using var domainResponse = await client.GetAsync("/domain");
        Assert.Equal(404, (int)domainResponse.StatusCode);
        Assert.Contains(log.Exceptions, e => e is InvalidOperationException);
#if NET10_0_OR_GREATER
        Assert.DoesNotContain(log.Exceptions, e => e is DomainException);
#else
        Assert.Contains(log.Exceptions, e => e is DomainException);
#endif
    }

    [Fact]
    public async Task Disabled_unexpected_handling_passes_to_next_handler()
    {
        await using var app = await Start(
            new OffsideAspNetCoreOptions { HandleUnexpectedExceptions = false },
            a => a.MapGet("/", (HttpContext _) => Throw(new InvalidOperationException())),
            s => s.AddExceptionHandler<NextHandler>());
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(418, (int)response.StatusCode);
    }

    [Fact]
    public async Task Started_response_is_skipped()
    {
        var http = new DefaultHttpContext();
        http.Features.Set<IHttpResponseFeature>(new StartedResponse());
        Assert.False(await new OffsideExceptionHandler().TryHandleAsync(
            http, new InvalidOperationException(), CancellationToken.None));
    }

    private static IResult Throw(Exception exception) => throw exception;

    private static async Task<WebApplication> Start(
        OffsideAspNetCoreOptions options, Action<WebApplication> endpoints,
        Action<IServiceCollection>? configure = null, ILoggerProvider? logging = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IErrorMessageResolver>(ProblemHttpHarness.Resolver);
        options.ResolveTraceId = _ => "test-trace";
        builder.Services.AddSingleton(options);
        builder.Services.AddOffsideExceptionHandler();
        configure?.Invoke(builder.Services);
        if (logging is not null)
            builder.Logging.AddProvider(logging);

        var app = builder.Build();
        app.UseOffsideExceptionHandler();
        endpoints(app);
        await app.StartAsync();
        return app;
    }

    private sealed class StartedResponse : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }

    private sealed class NextHandler : IExceptionHandler
    {
        public ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
        {
            http.Response.StatusCode = 418;
            return ValueTask.FromResult(true);
        }
    }

    private sealed class ExceptionLog : ILoggerProvider, ILogger
    {
        public System.Collections.Concurrent.ConcurrentQueue<Exception> Exceptions { get; } = new();
        public ILogger CreateLogger(string categoryName) => this;
        public void Dispose() { }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (exception is not null)
                Exceptions.Enqueue(exception);
        }
    }
}
