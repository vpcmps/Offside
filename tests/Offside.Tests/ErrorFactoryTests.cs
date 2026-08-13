using Offside;
using Xunit;

namespace Offside.Tests;

public sealed class ErrorFactoryTests
{
    [Fact]
    public void NotFound_sets_code_kind_and_arguments()
    {
        var error = Error.NotFound("order", 42);

        Assert.Equal("not_found", error.Code);
        Assert.Equal(ErrorKind.NotFound, error.Kind);
        Assert.Equal("order", error.Arguments["resource"]);
        Assert.Equal(42, error.Arguments["id"]);
        Assert.Null(error.Field);
    }

    [Fact]
    public void Custom_accepts_anonymous_arguments()
    {
        var orderId = "abc";
        var error = Error.Custom(
            "order.already_shipped",
            ErrorKind.Conflict,
            new { orderId });

        Assert.Equal("order.already_shipped", error.Code);
        Assert.Equal(ErrorKind.Conflict, error.Kind);
        Assert.Equal("abc", error.Arguments["orderId"]);
    }

    [Theory]
    [InlineData(nameof(ErrorKind.Gone), "gone", ErrorKind.Gone)]
    [InlineData(nameof(ErrorKind.Conflict), "conflict", ErrorKind.Conflict)]
    [InlineData(nameof(ErrorKind.BadRequest), "bad_request", ErrorKind.BadRequest)]
    [InlineData(nameof(ErrorKind.Unauthorized), "unauthorized", ErrorKind.Unauthorized)]
    [InlineData(nameof(ErrorKind.Forbidden), "forbidden", ErrorKind.Forbidden)]
    [InlineData(nameof(ErrorKind.PreconditionFailed), "precondition_failed", ErrorKind.PreconditionFailed)]
    [InlineData(nameof(ErrorKind.Unprocessable), "unprocessable", ErrorKind.Unprocessable)]
    [InlineData(nameof(ErrorKind.TooManyRequests), "too_many_requests", ErrorKind.TooManyRequests)]
    [InlineData(nameof(ErrorKind.Unexpected), "unexpected", ErrorKind.Unexpected)]
    public void Built_in_factory_sets_default_code_and_kind(
        string factory,
        string code,
        ErrorKind kind)
    {
        var error = factory switch
        {
            nameof(ErrorKind.Gone) => Error.Gone("order", 1),
            nameof(ErrorKind.Conflict) => Error.Conflict("order", "duplicate"),
            nameof(ErrorKind.BadRequest) => Error.BadRequest("malformed"),
            nameof(ErrorKind.Unauthorized) => Error.Unauthorized("token"),
            nameof(ErrorKind.Forbidden) => Error.Forbidden("role"),
            nameof(ErrorKind.PreconditionFailed) => Error.PreconditionFailed("etag"),
            nameof(ErrorKind.Unprocessable) => Error.Unprocessable("state"),
            nameof(ErrorKind.TooManyRequests) => Error.TooManyRequests("limit"),
            nameof(ErrorKind.Unexpected) => Error.Unexpected("boom"),
            _ => throw new ArgumentOutOfRangeException(nameof(factory))
        };

        Assert.Equal(code, error.Code);
        Assert.Equal(kind, error.Kind);
    }

    [Fact]
    public void Validation_uses_field_and_optional_code()
    {
        var error = Error.Validation("email", "email.taken", "a@b.c");

        Assert.Equal("email.taken", error.Code);
        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.Equal("email", error.Field);
        Assert.Equal("email", error.Arguments["field"]);
        Assert.Equal("a@b.c", error.Arguments["attemptedValue"]);
    }

    [Fact]
    public void Validation_without_code_uses_validation()
    {
        var error = Error.Validation("email");

        Assert.Equal("validation", error.Code);
        Assert.Equal("email", error.Field);
    }
}
