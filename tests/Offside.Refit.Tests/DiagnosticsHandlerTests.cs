using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Offside.Testing;
using Xunit;

namespace Offside.Refit.Tests;

public class DiagnosticsHandlerTests
{
    private sealed class RecordingObserver : IExternalApiErrorObserver
    {
        public List<Error> Observed { get; } = new();

        public void Observe(Error error) => Observed.Add(error);

        /// <summary>The observed errors as a failed result, so the Offside assertions apply.</summary>
        public Result AsResult() => Result.Failure(Observed);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _respond;

        public StubHandler(Func<HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(_respond());
    }

    private static HttpClient Client(RecordingObserver observer, Func<HttpResponseMessage> respond) =>
        new HttpClient(new OffsideRefitDiagnosticsHandler(observer, new OffsideRefitOptions())
        {
            InnerHandler = new StubHandler(respond)
        });

    [Fact]
    public async Task A_failed_response_is_observed_and_still_returned()
    {
        var observer = new RecordingObserver();
        using var client = Client(observer, () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var response = await client.GetAsync("https://payments.example/orders/42");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        observer.AsResult()
            .ShouldHaveOnlyError("external_api.service_unavailable")
            .WithKind(ErrorKind.ServiceUnavailable)
            .WithArgument("requestUri", "https://payments.example/orders/42")
            .WithArgument("status", 503);
    }

    [Fact]
    public async Task A_successful_response_is_not_observed()
    {
        var observer = new RecordingObserver();
        using var client = Client(observer, () => new HttpResponseMessage(HttpStatusCode.OK));

        await client.GetAsync("https://payments.example/orders/42");

        Assert.Empty(observer.Observed);
    }

    [Fact]
    public async Task A_transport_failure_is_observed_and_rethrown()
    {
        var observer = new RecordingObserver();
        using var client = Client(observer, () => throw new HttpRequestException("no route to host"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync("https://payments.example/orders/42"));

        observer.AsResult()
            .ShouldHaveOnlyError("external_api.service_unavailable")
            .WithKind(ErrorKind.ServiceUnavailable);
    }

    [Fact]
    public void The_registration_keeps_an_observer_the_host_already_registered()
    {
        var observer = new RecordingObserver();

        var provider = new ServiceCollection()
            .AddSingleton<IExternalApiErrorObserver>(observer)
            .AddOffsideRefitDiagnostics()
            .BuildServiceProvider();

        Assert.Same(observer, provider.GetRequiredService<IExternalApiErrorObserver>());
        Assert.NotNull(provider.GetRequiredService<OffsideRefitDiagnosticsHandler>());
    }

    [Fact]
    public void The_registration_falls_back_to_a_no_op_observer()
    {
        var provider = new ServiceCollection()
            .AddOffsideRefitDiagnostics()
            .BuildServiceProvider();

        var observer = provider.GetRequiredService<IExternalApiErrorObserver>();

        observer.Observe(Error.NotFound("order", 42));
        Assert.NotNull(provider.GetRequiredService<IExternalApiCaller>());
    }
}
