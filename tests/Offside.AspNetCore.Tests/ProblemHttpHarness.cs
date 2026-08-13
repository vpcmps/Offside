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
        Execute(result.ToHttpResult(Resolver, CultureInfo.InvariantCulture, exposeExceptionDetails: expose));

    internal static Task<ProblemPayload> Execute(Result result, OffsideAspNetCoreOptions options) =>
        Execute(result.ToHttpResult(Resolver, CultureInfo.InvariantCulture, options));

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
              "unexpected": "An unexpected error occurred.",
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
