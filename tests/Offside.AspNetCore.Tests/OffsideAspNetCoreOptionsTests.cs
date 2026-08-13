using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Offside.AspNetCore;
using Xunit;

namespace Offside.AspNetCore.Tests;

public sealed class OffsideAspNetCoreOptionsTests
{
    [Fact]
    public void FromEnvironment_sets_ExposeExceptionDetails_in_Development()
    {
        var options = OffsideAspNetCoreOptions.FromEnvironment(
            new StubHostEnvironment { EnvironmentName = Environments.Development });

        Assert.True(options.ExposeExceptionDetails);
    }

    [Fact]
    public void FromEnvironment_hides_details_in_Production()
    {
        var options = OffsideAspNetCoreOptions.FromEnvironment(
            new StubHostEnvironment { EnvironmentName = Environments.Production });

        Assert.False(options.ExposeExceptionDetails);
    }

    [Fact]
    public void AddOffsideAspNetCore_defaults_ExposeExceptionDetails_from_host()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(
            new StubHostEnvironment { EnvironmentName = Environments.Development });
        services.AddOffsideAspNetCore();

        var options = services.BuildServiceProvider()
            .GetRequiredService<OffsideAspNetCoreOptions>();

        Assert.True(options.ExposeExceptionDetails);
    }

    [Fact]
    public void AddOffsideAspNetCore_hides_details_without_host()
    {
        var services = new ServiceCollection();
        services.AddOffsideAspNetCore();

        var options = services.BuildServiceProvider()
            .GetRequiredService<OffsideAspNetCoreOptions>();

        Assert.False(options.ExposeExceptionDetails);
    }

    [Fact]
    public async Task ToHttpResult_HttpContext_resolves_resolver_and_options()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IErrorMessageResolver>(ProblemHttpHarness.Resolver);
        services.AddSingleton(new OffsideAspNetCoreOptions { ExposeExceptionDetails = true });
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.Response.Body = new MemoryStream();

        var result = Result.Failure(Error.Unexpected("secret-stack"));
        await result.ToHttpResult(httpContext).ExecuteAsync(httpContext);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var payload = await JsonSerializer.DeserializeAsync<ProblemPayload>(
            httpContext.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(500, payload!.Status);
        Assert.Equal("secret-stack", payload.Debug);
    }
}

