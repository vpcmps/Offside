using System.Net;
using Offside.Testing;
using Xunit;

namespace Offside.Refit.Tests;

public class ApiExceptionMappingTests
{
    private static OffsideRefitOptions Mirror(Action<OffsideRefitOptions>? configure = null)
    {
        var options = new OffsideRefitOptions { InboundStatus = InboundStatusMapping.Mirror };
        configure?.Invoke(options);
        return options;
    }

    [Fact]
    public void Without_a_body_the_status_decides()
    {
        ApiExceptionFactory.Create(HttpStatusCode.NotFound)
            .ToResult(Mirror())
            .ShouldHaveOnlyError("external_api.not_found")
            .WithKind(ErrorKind.NotFound)
            .WithErrorCode("NOT_FOUND")
            .WithArgument("status", 404)
            .WithArgument("api", "external api")
            .WithArgument("requestUri", ApiExceptionFactory.RequestUri);
    }

    [Fact]
    public void An_empty_code_prefix_falls_back_to_the_core_catalog_codes()
    {
        var options = new OffsideRefitOptions { CodePrefix = string.Empty };

        ApiExceptionFactory.Create(HttpStatusCode.GatewayTimeout)
            .ToResult(options)
            .ShouldHaveOnlyError("timeout")
            .WithKind(ErrorKind.Timeout);
    }

    [Fact]
    public void An_offside_problem_body_is_restored_error_for_error()
    {
        const string body = """
        {
          "type": "https://httpstatuses.io/409",
          "title": "Conflict",
          "status": 409,
          "detail": "Order already shipped.",
          "errorCode": "ORDER_ALREADY_SHIPPED",
          "errors": [
            { "code": "order.already_shipped", "errorCode": "ORDER_ALREADY_SHIPPED", "kind": "Conflict", "detail": "Order already shipped.", "field": null },
            { "code": "order.locked", "errorCode": "ORDER_LOCKED", "kind": "PreconditionFailed", "detail": "Order is locked.", "field": "orderId" }
          ]
        }
        """;

        var result = ApiExceptionFactory.Create(HttpStatusCode.Conflict, body)
            .ToResult(Mirror())
            .ShouldHaveErrorsInOrder("external_api.order.already_shipped", "external_api.order.locked");

        result.ShouldHaveError("external_api.order.already_shipped")
            .WithKind(ErrorKind.Conflict)
            .WithErrorCode("ORDER_ALREADY_SHIPPED")
            .ForField(null)
            .WithArgument("reason", "Order already shipped.");

        result.ShouldHaveError("external_api.order.locked")
            .WithKind(ErrorKind.PreconditionFailed)
            .WithErrorCode("ORDER_LOCKED")
            .ForField("orderId");
    }

    [Fact]
    public void An_unknown_kind_in_the_body_falls_back_to_the_status_kind()
    {
        const string body = """{ "errors": [ { "code": "weird", "kind": "Teleported" } ] }""";

        ApiExceptionFactory.Create(HttpStatusCode.Conflict, body)
            .ToResult(Mirror())
            .ShouldHaveOnlyError("external_api.weird")
            .WithKind(ErrorKind.Conflict);
    }

    [Fact]
    public void An_aspnet_validation_body_becomes_one_error_per_field()
    {
        const string body = """
        {
          "status": 400,
          "errors": { "email": ["Email is required."], "age": ["Must be positive."] }
        }
        """;

        var result = ApiExceptionFactory.Create(HttpStatusCode.BadRequest, body)
            .ToResult(Mirror())
            .ShouldHaveErrorCount(2);

        Assert.All(result.Errors, error => Assert.Equal(ErrorKind.Validation, error.Kind));
        Assert.Equal(new[] { "email", "age" }, result.Errors.Select(error => error.Field));
        Assert.Equal("Email is required.", result.Errors[0].Arguments["reason"]);
    }

    [Fact]
    public void A_plain_problem_body_keeps_the_partner_error_code()
    {
        const string body = """{ "detail": "Rate limit reached.", "errorCode": "PARTNER_QUOTA" }""";

        ApiExceptionFactory.Create(HttpStatusCode.TooManyRequests, body)
            .ToResult(Mirror())
            .ShouldHaveOnlyError("external_api.too_many_requests")
            .WithKind(ErrorKind.TooManyRequests)
            .WithErrorCode("PARTNER_QUOTA")
            .WithArgument("reason", "Rate limit reached.");
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{ \"errors\": 42 }")]
    [InlineData("")]
    public void A_body_that_cannot_be_read_degrades_to_the_status_mapping(string body)
    {
        ApiExceptionFactory.Create(HttpStatusCode.ServiceUnavailable, body)
            .ToResult()
            .ShouldHaveOnlyError("external_api.service_unavailable")
            .WithKind(ErrorKind.ServiceUnavailable);
    }

    [Fact]
    public void The_body_is_ignored_when_reading_problem_details_is_off()
    {
        const string body = """{ "detail": "Order already shipped.", "errorCode": "ORDER_ALREADY_SHIPPED" }""";
        var options = Mirror(o => o.ReadProblemDetails = false);

        ApiExceptionFactory.Create(HttpStatusCode.Conflict, body)
            .ToResult(options)
            .ShouldHaveOnlyError("external_api.conflict")
            .WithErrorCode("CONFLICT");
    }

    [Fact]
    public void The_generic_overload_carries_the_same_errors()
    {
        ApiExceptionFactory.Create(HttpStatusCode.NotFound)
            .ToResult<string>(Mirror())
            .ShouldBeFailure()
            .ShouldHaveOnlyError("external_api.not_found");
    }

    [Fact]
    public void ToError_returns_the_primary_error()
    {
        var error = ApiExceptionFactory.Create(HttpStatusCode.Forbidden).ToError(Mirror());

        Assert.Equal(ErrorKind.Forbidden, error.Kind);
        Assert.Equal("external_api.forbidden", error.Code);
    }

    [Fact]
    public void The_api_name_reaches_the_arguments()
    {
        var options = Mirror(o => o.ApiName = "payments");

        ApiExceptionFactory.Create(HttpStatusCode.Forbidden)
            .ToResult(options)
            .ShouldHaveOnlyError("external_api.forbidden")
            .WithArgument("api", "payments");
    }

    [Fact]
    public void A_null_exception_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => ((global::Refit.ApiException)null!).ToOffsideErrors());
    }
}
