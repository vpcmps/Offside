using System.Globalization;
using System.Text;
using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Offside.AspNetCore;
using Offside.FastEndpoint;
using Xunit;

namespace Offside.FastEndpoint.Tests;

public sealed class OffsideValidationResponseTests
{
    [Fact]
    public void Create_maps_failures_to_offside_problem()
    {
        var http = CreateHttp();
        var failures = new List<ValidationFailure>
        {
            new("Email", "taken") { ErrorCode = "email.taken", AttemptedValue = "a@b.c" }
        };

        var problem = OffsideValidationResponse.Create(failures, http);

        Assert.Equal(400, problem.Status);
        Assert.Equal("email.taken", problem.Errors[0].Code);
        Assert.Equal("VALIDATION", problem.ErrorCode);
        Assert.Equal("VALIDATION", problem.Errors[0].ErrorCode);
        Assert.Equal("Email", problem.Errors[0].Field);
    }

    [Fact]
    public void Create_applies_customize_problem_and_trace_id_from_options()
    {
        var services = new ServiceCollection();
        services.AddOffside(options =>
        {
            options.AddJson(
                CultureInfo.InvariantCulture,
                """{ "validation": "{field}", "email.taken": "taken {attemptedValue}" }""");
        });
        services.AddOffsideAspNetCore(options =>
        {
            options.CustomizeProblem = (problem, _) => problem.Extensions["message"] = "legacy";
            options.ResolveTraceId = _ => "fe-trace";
        });

        var http = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var failures = new List<ValidationFailure>
        {
            new("Email", "taken") { ErrorCode = "email.taken" }
        };

        var problem = OffsideValidationResponse.Create(failures, http);

        Assert.Equal("legacy", problem.Extensions["message"]);
        Assert.Equal("fe-trace", problem.TraceId);
    }

    [Fact]
    public void Create_empty_failures_still_returns_a_validation_problem()
    {
        var http = CreateHttp();

        var problem = OffsideValidationResponse.Create([], http);

        Assert.Equal(400, problem.Status);
        Assert.Equal("validation", problem.Errors[0].Code);
    }

    private static DefaultHttpContext CreateHttp()
    {
        var services = new ServiceCollection();
        services.AddOffside(options =>
        {
            options.AddJson(
                CultureInfo.InvariantCulture,
                """{ "validation": "{field}", "email.taken": "taken {attemptedValue}" }""");
        });
        services.AddOffsideAspNetCore();

        return new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            TraceIdentifier = "trace-1"
        };
    }
}

public sealed class OffsideResultSendTests
{
    [Fact]
    public async Task SendOffsideAsync_writes_problem_json_on_failure()
    {
        var http = CreateHttp();
        http.Response.Body = new MemoryStream();

        await Result.Failure(Error.NotFound("order", 1)).SendOffsideAsync(http);

        Assert.Equal(404, http.Response.StatusCode);
        Assert.Contains("application/problem+json", http.Response.ContentType);

        http.Response.Body.Seek(0, SeekOrigin.Begin);
        using var doc = await JsonDocument.ParseAsync(http.Response.Body);
        Assert.Equal("NOT_FOUND", doc.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal("not_found", doc.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task SendOffsideAsync_writes_no_content_on_unit_success()
    {
        var http = CreateHttp();
        http.Response.Body = new MemoryStream();

        await Result.Success().SendOffsideAsync(http);

        Assert.Equal(204, http.Response.StatusCode);
    }

    [Fact]
    public async Task SendOffsideAsync_writes_value_on_success()
    {
        var http = CreateHttp();
        http.Response.Body = new MemoryStream();

        await Result<int>.Success(42).SendOffsideAsync(http);

        Assert.Equal(200, http.Response.StatusCode);
        http.Response.Body.Seek(0, SeekOrigin.Begin);
        Assert.Equal("42", Encoding.UTF8.GetString(((MemoryStream)http.Response.Body).ToArray()));
    }

    private static DefaultHttpContext CreateHttp()
    {
        var services = new ServiceCollection();
        services.AddOffside(options =>
        {
            options.AddJson(
                CultureInfo.InvariantCulture,
                """{ "not_found": "{resource}" }""");
        });
        services.AddOffsideAspNetCore();
        services.AddLogging();

        return new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }
}

public sealed class UseOffsideTests
{
    [Fact]
    public async Task Validation_failure_returns_offside_problem()
    {
        using var factory = new FastEndpointTestFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/users", new StringContent("""{"email":""}""", Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("VALIDATION", doc.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal("email.required", doc.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
        Assert.Equal("email", doc.RootElement.GetProperty("errors")[0].GetProperty("field").GetString());
    }

    [Fact]
    public void Configurator_adds_offside_status_metadata()
    {
        using var factory = new FastEndpointTestFactory();
        factory.CreateClient();

        var endpoint = factory.Services
            .GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>()
            .Endpoints
            .Single(e => HasRoute(e, "/users"));

        var statuses = endpoint.Metadata
            .GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Where(m => m.Type == typeof(OffsideProblem))
            .Select(m => m.StatusCode)
            .Distinct()
            .OrderBy(s => s)
            .ToArray();

        Assert.Equal(OffsideHttp.StatusCodes, statuses);
    }

    [Fact]
    public void DontProduceOffside_omits_offside_status_metadata()
    {
        using var factory = new FastEndpointTestFactory();
        factory.CreateClient();

        var endpoint = factory.Services
            .GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>()
            .Endpoints
            .Single(e => HasRoute(e, "/health"));

        var offside = endpoint.Metadata
            .GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Where(m => m.Type == typeof(OffsideProblem));

        Assert.Empty(offside);
    }

    [Fact]
    public async Task SendOffsideAsync_from_handler_returns_not_found()
    {
        using var factory = new FastEndpointTestFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/orders/missing");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("NOT_FOUND", doc.RootElement.GetProperty("errorCode").GetString());
    }

    private static bool HasRoute(Microsoft.AspNetCore.Http.Endpoint endpoint, string route) =>
        endpoint is Microsoft.AspNetCore.Routing.RouteEndpoint routed
        && routed.RoutePattern.RawText is not null
        && routed.RoutePattern.RawText.Contains(route, StringComparison.Ordinal);
}
