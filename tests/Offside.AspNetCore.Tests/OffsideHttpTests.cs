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
    public void StatusCode_maps_every_kind(ErrorKind kind, int status)
    {
        Assert.Equal(status, OffsideHttp.StatusCode(kind));
    }

    [Fact]
    public void StatusCodes_are_the_distinct_offside_statuses()
    {
        Assert.Equal(
            new[] { 400, 401, 403, 404, 409, 410, 412, 422, 429, 500 },
            OffsideHttp.StatusCodes);
    }
}
