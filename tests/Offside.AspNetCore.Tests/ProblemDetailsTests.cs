using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Offside.AspNetCore;
using Xunit;

namespace Offside.AspNetCore.Tests;

public sealed class ProblemDetailsTests
{
    private static readonly JsonErrorMessageResolver Resolver = CreateResolver();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Mixed_kinds_use_most_severe_status()
    {
        var result = Result.Failure(
            Error.Validation("email"),
            Error.Conflict("order", "dup"),
            Error.NotFound("order", 1));

        var payload = await Execute(result);

        Assert.Equal(409, payload.Status);
        Assert.Equal("https://httpstatuses.io/409", payload.Type);
        Assert.Equal("Conflict", payload.Title);
        Assert.Equal(3, payload.Errors.Count);
    }

    [Fact]
    public async Task Tie_Unauthorized_Forbidden_uses_first_in_list()
    {
        var forbiddenFirst = Result.Failure(
            Error.Forbidden(),
            Error.Unauthorized());

        var payload = await Execute(forbiddenFirst);

        Assert.Equal(403, payload.Status);
    }

    [Fact]
    public async Task Primary_detail_is_first_error_of_winning_kind()
    {
        var result = Result.Failure(
            Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId = "123" }),
            Error.Validation("email"));

        var payload = await Execute(result);

        Assert.Equal("order.already_shipped", payload.Errors[0].Code);
        Assert.NotNull(payload.TraceId);
    }

    [Fact]
    public async Task ToActionResult_writes_problem_json()
    {
        var result = Result.Failure(Error.NotFound("order", 1));
        var action = result.ToActionResult(Resolver, CultureInfo.InvariantCulture);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        await action.ExecuteResultAsync(actionContext);

        Assert.Contains("application/problem+json", httpContext.Response.ContentType);
    }

    private static async Task<ProblemPayload> Execute(Result result)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var httpResult = result.ToHttpResult(Resolver, CultureInfo.InvariantCulture);
        await httpResult.ExecuteAsync(httpContext);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var payload = await JsonSerializer.DeserializeAsync<ProblemPayload>(
            httpContext.Response.Body,
            JsonOptions);

        Assert.NotNull(payload);
        return payload;
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
              "unexpected": "oops",
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

    private sealed class ProblemPayload
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int Status { get; set; }
        public string? Detail { get; set; }
        public string? TraceId { get; set; }
        public List<ProblemErrorPayload> Errors { get; set; } = [];
    }

    private sealed class ProblemErrorPayload
    {
        public string? Code { get; set; }
        public string? Kind { get; set; }
        public string? Detail { get; set; }
        public string? Field { get; set; }
    }
}
