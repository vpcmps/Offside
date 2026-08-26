using System.Net;
using Xunit;

namespace Offside.Refit.Tests;

public class StatusMappingTests
{
    [Theory]
    [InlineData(400, ErrorKind.BadRequest)]
    [InlineData(401, ErrorKind.Unauthorized)]
    [InlineData(403, ErrorKind.Forbidden)]
    [InlineData(404, ErrorKind.NotFound)]
    [InlineData(409, ErrorKind.Conflict)]
    [InlineData(410, ErrorKind.Gone)]
    [InlineData(412, ErrorKind.PreconditionFailed)]
    [InlineData(422, ErrorKind.Unprocessable)]
    [InlineData(429, ErrorKind.TooManyRequests)]
    [InlineData(500, ErrorKind.Unexpected)]
    [InlineData(502, ErrorKind.ServiceUnavailable)]
    [InlineData(503, ErrorKind.ServiceUnavailable)]
    [InlineData(504, ErrorKind.Timeout)]
    public void Maps_each_documented_status_to_its_kind(int status, ErrorKind expected) =>
        Assert.Equal(expected, OffsideRefit.Kind((HttpStatusCode)status));

    [Theory]
    [InlineData(402)]
    [InlineData(418)]
    [InlineData(451)]
    public void Unmapped_client_status_falls_back_to_bad_request(int status) =>
        Assert.Equal(ErrorKind.BadRequest, OffsideRefit.Kind((HttpStatusCode)status));

    [Theory]
    [InlineData(501)]
    [InlineData(505)]
    public void Unmapped_server_status_falls_back_to_unexpected(int status) =>
        Assert.Equal(ErrorKind.Unexpected, OffsideRefit.Kind((HttpStatusCode)status));

    [Fact]
    public void Every_kind_has_a_catalog_code_suffix()
    {
        foreach (ErrorKind kind in Enum.GetValues(typeof(ErrorKind)))
            Assert.False(string.IsNullOrWhiteSpace(OffsideRefit.CodeSuffix(kind)));
    }
}
