using System.Net;
using Offside.Testing;
using Xunit;

namespace Offside.Refit.Tests;

public class InboundStatusMappingTests
{
    [Fact]
    public void Default_collapses_a_raw_404_to_service_unavailable()
    {
        ApiExceptionFactory.Create(HttpStatusCode.NotFound)
            .ToResult()
            .ShouldHaveOnlyError("external_api.service_unavailable")
            .WithKind(ErrorKind.ServiceUnavailable)
            .WithArgument("originalKind", "NotFound")
            .WithArgument("originalCode", "external_api.not_found")
            .WithArgument("status", 404);
    }

    [Fact]
    public void Default_collapses_an_offside_problem_not_found()
    {
        const string body = """
        {
          "status": 404,
          "errors": [
            { "code": "not_found", "errorCode": "NOT_FOUND", "kind": "NotFound", "detail": "missing." }
          ]
        }
        """;

        ApiExceptionFactory.Create(HttpStatusCode.NotFound, body)
            .ToResult()
            .ShouldHaveOnlyError("external_api.service_unavailable")
            .WithKind(ErrorKind.ServiceUnavailable)
            .WithArgument("originalKind", "NotFound")
            .WithArgument("originalCode", "external_api.not_found");
    }

    [Fact]
    public void Mirror_keeps_the_dependency_kind()
    {
        var options = new OffsideRefitOptions { InboundStatus = InboundStatusMapping.Mirror };

        ApiExceptionFactory.Create(HttpStatusCode.NotFound)
            .ToResult(options)
            .ShouldHaveOnlyError("external_api.not_found")
            .WithKind(ErrorKind.NotFound);

        const string body = """
        {
          "status": 409,
          "errors": [
            { "code": "order.already_shipped", "errorCode": "ORDER_ALREADY_SHIPPED", "kind": "Conflict" }
          ]
        }
        """;

        ApiExceptionFactory.Create(HttpStatusCode.Conflict, body)
            .ToResult(options)
            .ShouldHaveOnlyError("external_api.order.already_shipped")
            .WithKind(ErrorKind.Conflict);
    }

    [Fact]
    public void Timeout_and_service_unavailable_are_left_alone()
    {
        ApiExceptionFactory.Create(HttpStatusCode.GatewayTimeout)
            .ToResult()
            .ShouldHaveOnlyError("external_api.timeout")
            .WithKind(ErrorKind.Timeout);

        ApiExceptionFactory.Create(HttpStatusCode.ServiceUnavailable)
            .ToResult()
            .ShouldHaveOnlyError("external_api.service_unavailable")
            .WithKind(ErrorKind.ServiceUnavailable);
    }
}
