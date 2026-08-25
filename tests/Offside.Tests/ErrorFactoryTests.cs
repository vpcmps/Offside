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
        Assert.Equal("NOT_FOUND", error.ErrorCode);
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
        Assert.Equal("CONFLICT", error.ErrorCode);
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
    [InlineData(nameof(ErrorKind.ServiceUnavailable), "service_unavailable", ErrorKind.ServiceUnavailable)]
    [InlineData(nameof(ErrorKind.Timeout), "timeout", ErrorKind.Timeout)]
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
            nameof(ErrorKind.ServiceUnavailable) => Error.ServiceUnavailable("otp"),
            nameof(ErrorKind.Timeout) => Error.Timeout("upstream"),
            nameof(ErrorKind.Unexpected) => Error.Unexpected("boom"),
            _ => throw new ArgumentOutOfRangeException(nameof(factory))
        };

        Assert.Equal(code, error.Code);
        Assert.Equal(kind, error.Kind);
        Assert.Equal(Error.DefaultErrorCode(kind), error.ErrorCode);
    }

    [Fact]
    public void Validation_uses_field_and_optional_code()
    {
        var error = Error.Validation("email", "email.taken", "a@b.c");

        Assert.Equal("email.taken", error.Code);
        Assert.Equal("VALIDATION", error.ErrorCode);
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
        Assert.Equal("VALIDATION", error.ErrorCode);
        Assert.Equal("email", error.Field);
    }

    [Fact]
    public void Custom_override_sets_error_code()
    {
        var error = Error.Custom(
            "order.already_shipped",
            ErrorKind.Conflict,
            new { orderId = "1" },
            errorCode: "ORDER_ALREADY_SHIPPED");

        Assert.Equal("order.already_shipped", error.Code);
        Assert.Equal("ORDER_ALREADY_SHIPPED", error.ErrorCode);
    }

    [Fact]
    public void Built_in_override_sets_error_code()
    {
        var error = Error.NotFound("order", 1, errorCode: "ORDER_NOT_FOUND");

        Assert.Equal("not_found", error.Code);
        Assert.Equal("ORDER_NOT_FOUND", error.ErrorCode);
    }

    [Fact]
    public void Blank_error_code_falls_back_to_kind_default()
    {
        var error = Error.Custom(
            "order.already_shipped",
            ErrorKind.Conflict,
            errorCode: "  ");

        Assert.Equal("CONFLICT", error.ErrorCode);
    }

    [Fact]
    public void Error_code_is_trimmed()
    {
        var error = Error.Custom(
            "order.already_shipped",
            ErrorKind.Conflict,
            errorCode: "  ORDER_ALREADY_SHIPPED  ");

        Assert.Equal("ORDER_ALREADY_SHIPPED", error.ErrorCode);
    }

    [Theory]
    [InlineData(ErrorKind.Unexpected, "UNEXPECTED")]
    [InlineData(ErrorKind.Unauthorized, "UNAUTHORIZED")]
    [InlineData(ErrorKind.Forbidden, "FORBIDDEN")]
    [InlineData(ErrorKind.TooManyRequests, "TOO_MANY_REQUESTS")]
    [InlineData(ErrorKind.Conflict, "CONFLICT")]
    [InlineData(ErrorKind.PreconditionFailed, "PRECONDITION_FAILED")]
    [InlineData(ErrorKind.Gone, "GONE")]
    [InlineData(ErrorKind.Unprocessable, "UNPROCESSABLE")]
    [InlineData(ErrorKind.NotFound, "NOT_FOUND")]
    [InlineData(ErrorKind.Validation, "VALIDATION")]
    [InlineData(ErrorKind.BadRequest, "BAD_REQUEST")]
    [InlineData(ErrorKind.ServiceUnavailable, "SERVICE_UNAVAILABLE")]
    [InlineData(ErrorKind.Timeout, "TIMEOUT")]
    public void DefaultErrorCode_maps_every_kind(ErrorKind kind, string errorCode)
    {
        Assert.Equal(errorCode, Error.DefaultErrorCode(kind));
    }
}
