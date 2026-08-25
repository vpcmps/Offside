using Offside.AspNetCore;
using Xunit;

namespace Offside.AspNetCore.Tests;

public sealed class OffsideHttpTests
{
    [Theory]
    [InlineData(ErrorKind.Unexpected, 500)]
    [InlineData(ErrorKind.Unauthorized, 401)]
    [InlineData(ErrorKind.Forbidden, 403)]
    [InlineData(ErrorKind.TooManyRequests, 429)]
    [InlineData(ErrorKind.Conflict, 409)]
    [InlineData(ErrorKind.PreconditionFailed, 412)]
    [InlineData(ErrorKind.Gone, 410)]
    [InlineData(ErrorKind.Unprocessable, 422)]
    [InlineData(ErrorKind.NotFound, 404)]
    [InlineData(ErrorKind.Validation, 400)]
    [InlineData(ErrorKind.BadRequest, 400)]
    [InlineData(ErrorKind.ServiceUnavailable, 503)]
    [InlineData(ErrorKind.Timeout, 504)]
    public void StatusCode_maps_every_kind(ErrorKind kind, int status)
    {
        Assert.Equal(status, OffsideHttp.StatusCode(kind));
    }

    [Fact]
    public void StatusCodes_are_the_distinct_offside_statuses()
    {
        Assert.Equal(
            new[] { 400, 401, 403, 404, 409, 410, 412, 422, 429, 500, 503, 504 },
            OffsideHttp.StatusCodes);
    }

    [Fact]
    public void SelectPrimary_unexpected_wins_over_timeout()
    {
        var primary = OffsideHttp.SelectPrimary(
        [
            Error.Timeout(),
            Error.Unexpected("boom")
        ]);

        Assert.Equal(ErrorKind.Unexpected, primary.Kind);
    }

    [Fact]
    public void SelectPrimary_unauthorized_wins_over_service_unavailable()
    {
        var primary = OffsideHttp.SelectPrimary(
        [
            Error.ServiceUnavailable(),
            Error.Unauthorized()
        ]);

        Assert.Equal(ErrorKind.Unauthorized, primary.Kind);
    }

    [Fact]
    public void SelectPrimary_too_many_requests_wins_over_timeout()
    {
        var primary = OffsideHttp.SelectPrimary(
        [
            Error.Timeout(),
            Error.TooManyRequests()
        ]);

        Assert.Equal(ErrorKind.TooManyRequests, primary.Kind);
    }

    [Fact]
    public void SelectPrimary_timeout_wins_over_validation()
    {
        var primary = OffsideHttp.SelectPrimary(
        [
            Error.Validation("email"),
            Error.Timeout()
        ]);

        Assert.Equal(ErrorKind.Timeout, primary.Kind);
    }

    [Fact]
    public void SelectPrimary_service_unavailable_wins_over_conflict()
    {
        var primary = OffsideHttp.SelectPrimary(
        [
            Error.Conflict("order"),
            Error.ServiceUnavailable()
        ]);

        Assert.Equal(ErrorKind.ServiceUnavailable, primary.Kind);
    }

    [Fact]
    public void SelectPrimary_tie_uses_first_in_list()
    {
        var primary = OffsideHttp.SelectPrimary(
        [
            Error.Timeout("first"),
            Error.ServiceUnavailable("second")
        ]);

        Assert.Equal(ErrorKind.Timeout, primary.Kind);
        Assert.Equal("first", primary.Arguments["reason"]);
    }

    [Fact]
    public void SelectPrimary_empty_throws()
    {
        Assert.Throws<ArgumentException>(() => OffsideHttp.SelectPrimary([]));
    }
}
