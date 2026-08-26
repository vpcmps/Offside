using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Offside.AspNetCore;
using Xunit;

namespace Offside.AspNetCore.Tests;

public sealed class ProblemDetailsTests
{
    [Fact]
    public async Task Mixed_kinds_use_most_severe_status()
    {
        var result = Result.Failure(
            Error.Validation("email"),
            Error.Conflict("order", "dup"),
            Error.NotFound("order", 1));

        var payload = await ProblemHttpHarness.Execute(result);

        Assert.Equal(409, payload.Status);
        Assert.Equal("https://httpstatuses.io/409", payload.Type);
        Assert.Equal("Conflict", payload.Title);
        Assert.Equal(3, payload.Errors.Count);
    }

    [Fact]
    public async Task Timeout_and_validation_use_504()
    {
        var result = Result.Failure(
            Error.Validation("email"),
            Error.Timeout());

        var payload = await ProblemHttpHarness.Execute(result);

        Assert.Equal(504, payload.Status);
        Assert.Equal("Timeout", payload.Title);
    }

    [Fact]
    public async Task Unauthorized_and_service_unavailable_use_401()
    {
        var result = Result.Failure(
            Error.ServiceUnavailable(),
            Error.Unauthorized());

        var payload = await ProblemHttpHarness.Execute(result);

        Assert.Equal(401, payload.Status);
    }

    [Fact]
    public async Task Tie_Unauthorized_Forbidden_uses_first_in_list()
    {
        var forbiddenFirst = Result.Failure(
            Error.Forbidden(),
            Error.Unauthorized());

        var payload = await ProblemHttpHarness.Execute(forbiddenFirst);

        Assert.Equal(403, payload.Status);
    }

    [Fact]
    public async Task Primary_detail_is_first_error_of_winning_kind()
    {
        var result = Result.Failure(
            Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId = "123" }),
            Error.Validation("email"));

        var payload = await ProblemHttpHarness.Execute(result);

        Assert.Equal("order.already_shipped", payload.Errors[0].Code);
        Assert.Equal("CONFLICT", payload.ErrorCode);
        Assert.Equal("CONFLICT", payload.Errors[0].ErrorCode);
        Assert.NotNull(payload.TraceId);
    }

    [Fact]
    public async Task Primary_error_code_uses_override()
    {
        var result = Result.Failure(
            Error.Custom(
                "order.already_shipped",
                ErrorKind.Conflict,
                new { orderId = "123" },
                errorCode: "ORDER_ALREADY_SHIPPED"),
            Error.Validation("email"));

        var payload = await ProblemHttpHarness.Execute(result);

        Assert.Equal("ORDER_ALREADY_SHIPPED", payload.ErrorCode);
        Assert.Equal("ORDER_ALREADY_SHIPPED", payload.Errors[0].ErrorCode);
        Assert.Equal("VALIDATION", payload.Errors[1].ErrorCode);
    }

    [Fact]
    public async Task ToActionResult_writes_problem_json()
    {
        var result = Result.Failure(Error.NotFound("order", 1));
        var action = result.ToActionResult(
            ProblemHttpHarness.Resolver,
            CultureInfo.InvariantCulture,
            new OffsideAspNetCoreOptions());

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        await action.ExecuteResultAsync(actionContext);

        Assert.Contains("application/problem+json", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task ToActionResultT_writes_problem_json_on_failure()
    {
        var result = Result<int>.Failure(Error.NotFound("order", 1));
        var action = result.ToActionResult(
            ProblemHttpHarness.Resolver,
            CultureInfo.InvariantCulture,
            new OffsideAspNetCoreOptions());

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        await action.ExecuteResultAsync(actionContext);

        Assert.Equal(404, httpContext.Response.StatusCode);
        Assert.Contains("application/problem+json", httpContext.Response.ContentType);
    }

    [Fact]
    public void ToActionResultT_returns_Ok_on_success()
    {
        var result = Result<int>.Success(42);
        var action = result.ToActionResult(
            ProblemHttpHarness.Resolver,
            CultureInfo.InvariantCulture,
            new OffsideAspNetCoreOptions());

        var ok = Assert.IsType<OkObjectResult>(action);
        Assert.Equal(42, ok.Value);
    }
}
