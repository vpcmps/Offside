using Xunit;

namespace Offside.AspNetCore.Tests;

public sealed class Sanitized500Tests
{
    [Fact]
    public async Task Unexpected_hides_internal_detail()
    {
        var result = Result.Failure(Error.Unexpected("secret-stack"));
        var payload = await Execute(result, expose: false);
        Assert.Equal(500, payload.Status);
        Assert.DoesNotContain("secret-stack", payload.Detail);
        Assert.Null(payload.Debug);
        Assert.Equal("UNEXPECTED", payload.ErrorCode);
        Assert.Equal("UNEXPECTED", payload.Errors[0].ErrorCode);
        Assert.NotNull(payload.TraceId);
    }

    [Fact]
    public async Task Unexpected_with_other_errors_still_sanitizes()
    {
        var result = Result.Failure(
            Error.Validation("email"),
            Error.Unexpected("secret"));
        var payload = await Execute(result, expose: false);
        Assert.Equal(500, payload.Status);
        Assert.DoesNotContain("secret", payload.Detail);
        Assert.Equal("UNEXPECTED", payload.ErrorCode);
    }

    [Fact]
    public async Task Unexpected_custom_error_code_is_sanitized()
    {
        var result = Result.Failure(
            Error.Custom("db.timeout", ErrorKind.Unexpected, errorCode: "DB_TIMEOUT"));
        var payload = await Execute(result, expose: false);

        Assert.Equal(500, payload.Status);
        Assert.Equal("UNEXPECTED", payload.ErrorCode);
        Assert.Equal("UNEXPECTED", payload.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task ExposeExceptionDetails_puts_message_in_debug_not_detail()
    {
        var result = Result.Failure(Error.Unexpected("secret-stack"));
        var payload = await Execute(result, expose: true);
        Assert.Equal(500, payload.Status);
        Assert.DoesNotContain("secret-stack", payload.Detail);
        Assert.Equal("secret-stack", payload.Debug);
    }

    [Fact]
    public async Task ToHttpResult_uses_options_ExposeExceptionDetails()
    {
        var result = Result.Failure(Error.Unexpected("secret-stack"));
        var options = new OffsideAspNetCoreOptions { ExposeExceptionDetails = true };
        var payload = await ProblemHttpHarness.Execute(result, options);

        Assert.Equal(500, payload.Status);
        Assert.DoesNotContain("secret-stack", payload.Detail);
        Assert.Equal("secret-stack", payload.Debug);
    }

    private static Task<ProblemPayload> Execute(Result result, bool expose) =>
        ProblemHttpHarness.Execute(result, expose);
}
