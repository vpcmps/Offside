using Xunit;

namespace Offside.Tests;

public sealed class DomainErrorSeverityMapTests
{
    [Theory]
    [InlineData(ErrorKind.NotFound, DomainErrorSeverity.Information)]
    [InlineData(ErrorKind.Validation, DomainErrorSeverity.Information)]
    [InlineData(ErrorKind.BadRequest, DomainErrorSeverity.Information)]
    [InlineData(ErrorKind.Unexpected, DomainErrorSeverity.Critical)]
    [InlineData(ErrorKind.ServiceUnavailable, DomainErrorSeverity.Error)]
    [InlineData(ErrorKind.Timeout, DomainErrorSeverity.Error)]
    [InlineData(ErrorKind.Conflict, DomainErrorSeverity.Warning)]
    public void Library_matches_the_published_defaults(ErrorKind kind, DomainErrorSeverity expected) =>
        Assert.Equal(expected, DomainErrorSeverityMap.Library(kind));

    [Theory]
    [InlineData(ErrorKind.NotFound, DomainErrorSeverity.Warning)]
    [InlineData(ErrorKind.Validation, DomainErrorSeverity.Warning)]
    [InlineData(ErrorKind.BadRequest, DomainErrorSeverity.Warning)]
    [InlineData(ErrorKind.Unexpected, DomainErrorSeverity.Error)]
    [InlineData(ErrorKind.ServiceUnavailable, DomainErrorSeverity.Error)]
    public void Operations_raises_refusals_and_drops_unexpected(ErrorKind kind, DomainErrorSeverity expected) =>
        Assert.Equal(expected, DomainErrorSeverityMap.Operations(kind));

    [Fact]
    public void Every_kind_is_mapped()
    {
        foreach (var kind in Enum.GetValues(typeof(ErrorKind)).Cast<ErrorKind>())
        {
            _ = DomainErrorSeverityMap.Library(kind);
            _ = DomainErrorSeverityMap.Operations(kind);
        }
    }
}
