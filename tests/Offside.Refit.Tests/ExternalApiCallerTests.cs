using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Offside.Testing;
using Xunit;

namespace Offside.Refit.Tests;

public class ExternalApiCallerTests
{
    private static IExternalApiCaller Caller(Action<OffsideRefitOptions>? configure = null) =>
        new ServiceCollection()
            .AddOffsideRefit(configure)
            .BuildServiceProvider()
            .GetRequiredService<IExternalApiCaller>();

    [Fact]
    public async Task A_successful_call_returns_the_value()
    {
        var result = await Caller().CallAsync(_ => Task.FromResult("ok"));

        result.ShouldBeSuccess().WithValue("ok");
    }

    [Fact]
    public async Task An_api_exception_becomes_a_failure()
    {
        var result = await Caller().CallAsync<string>(
            _ => throw ApiExceptionFactory.Create(HttpStatusCode.NotFound));

        result.ShouldBeFailure()
            .ShouldHaveOnlyError("external_api.not_found")
            .WithKind(ErrorKind.NotFound);
    }

    [Fact]
    public async Task A_transport_failure_becomes_service_unavailable()
    {
        var result = await Caller().CallAsync<string>(
            _ => throw new HttpRequestException("no route to host"));

        result.ShouldHaveOnlyError("external_api.service_unavailable")
            .WithKind(ErrorKind.ServiceUnavailable)
            .WithArgument("reason", "no route to host");
    }

    [Fact]
    public async Task A_timeout_becomes_a_timeout_error()
    {
        var result = await Caller().CallAsync<string>(_ => throw new TimeoutException("too slow"));

        result.ShouldHaveOnlyError("external_api.timeout").WithKind(ErrorKind.Timeout);
    }

    [Fact]
    public async Task A_cancellation_the_caller_did_not_request_becomes_a_timeout_error()
    {
        var result = await Caller().CallAsync<string>(
            _ => throw new TaskCanceledException("the request timed out"));

        result.ShouldHaveOnlyError("external_api.timeout").WithKind(ErrorKind.Timeout);
    }

    [Fact]
    public async Task A_cancellation_the_caller_requested_is_rethrown()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Caller().CallAsync<string>(
                token => throw new TaskCanceledException("cancelled", null, token),
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task An_unrelated_exception_is_not_swallowed()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Caller().CallAsync<string>(_ => throw new InvalidOperationException("a bug of my own")));
    }

    [Fact]
    public async Task The_void_overload_reports_success_and_failure()
    {
        var success = await Caller().CallAsync(_ => Task.CompletedTask);
        var failure = await Caller().CallAsync(
            _ => throw ApiExceptionFactory.Create(HttpStatusCode.Conflict));

        success.ShouldBeSuccess();
        failure.ShouldHaveOnlyError("external_api.conflict").WithKind(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Registered_options_apply_to_every_call()
    {
        var caller = Caller(options => options.ApiName = "payments");

        var result = await caller.CallAsync<string>(
            _ => throw ApiExceptionFactory.Create(HttpStatusCode.NotFound));

        result.ShouldHaveOnlyError("external_api.not_found").WithArgument("api", "payments");
    }

    [Fact]
    public async Task Per_call_options_win_over_the_registered_ones()
    {
        var caller = Caller(options => options.ApiName = "payments");

        var result = await caller.CallAsync<string>(
            _ => throw ApiExceptionFactory.Create(HttpStatusCode.NotFound),
            new OffsideRefitOptions { ApiName = "shipping" });

        result.ShouldHaveOnlyError("external_api.not_found").WithArgument("api", "shipping");
    }

    [Fact]
    public async Task The_cancellation_token_reaches_the_call()
    {
        using var cancellation = new CancellationTokenSource();
        CancellationToken observed = default;

        await Caller().CallAsync(
            token =>
            {
                observed = token;
                return Task.FromResult(0);
            },
            cancellationToken: cancellation.Token);

        Assert.Equal(cancellation.Token, observed);
    }
}
