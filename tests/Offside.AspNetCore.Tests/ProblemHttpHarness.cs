using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Offside.AspNetCore;
using Xunit;

namespace Offside.AspNetCore.Tests;

internal static class ProblemHttpHarness
{
    internal static readonly JsonErrorMessageResolver Resolver = CreateResolver();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static Task<ProblemPayload> Execute(Result result, bool expose = false) =>
        Execute(result, new OffsideAspNetCoreOptions { ExposeExceptionDetails = expose });

    internal static Task<ProblemPayload> Execute(Result result, OffsideAspNetCoreOptions options) =>
        Execute(result, options, configure: null);

    internal static async Task<ProblemPayload> Execute(
        Result result,
        OffsideAspNetCoreOptions options,
        Action<DefaultHttpContext>? configure)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        configure?.Invoke(httpContext);
        await result.ToHttpResult(Resolver, CultureInfo.InvariantCulture, options).ExecuteAsync(httpContext);
        return await ReadPayload(httpContext);
    }

    internal static async Task<(HttpContext Http, string Body)> ExecuteRaw(
        Result result,
        OffsideAspNetCoreOptions options,
        Action<DefaultHttpContext>? configure = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        configure?.Invoke(httpContext);
        await result.ToHttpResult(Resolver, CultureInfo.InvariantCulture, options).ExecuteAsync(httpContext);
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        return (httpContext, body);
    }

    private static async Task<ProblemPayload> Execute(IResult httpResult)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        await httpResult.ExecuteAsync(httpContext);
        return await ReadPayload(httpContext);
    }

    private static async Task<ProblemPayload> ReadPayload(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var payload = await JsonSerializer.DeserializeAsync<ProblemPayload>(
            httpContext.Response.Body,
            JsonOptions);

        Assert.NotNull(payload);
        return payload!;
    }

    private static JsonErrorMessageResolver CreateResolver()
    {
        const string json =
            """
            {
              "not_found": "{resource}",
              "conflict": "conflict {resource}",
              "validation": "{field}",
              "unauthorized": "unauth",
              "forbidden": "forbid",
              "too_many_requests": "slow down",
              "unexpected": "An unexpected error occurred.",
              "service_unavailable": "The service is temporarily unavailable.",
              "timeout": "The request timed out.",
              "order.already_shipped": "shipped {orderId}"
            }
            """;

        return new JsonErrorMessageResolver(
        [
            new JsonErrorCatalog(
                CultureInfo.InvariantCulture,
                new MemoryStream(Encoding.UTF8.GetBytes(json)))
        ]);
    }
}
