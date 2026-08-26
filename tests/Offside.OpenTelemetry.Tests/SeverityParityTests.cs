using Xunit;

namespace Offside.OpenTelemetry.Tests;

public class SeverityParityTests
{
    [Fact]
    public void Both_integrations_agree_on_every_kind()
    {
        var openTelemetry = new OffsideOpenTelemetryOptions();
        var applicationInsights = new Offside.ApplicationInsights.OffsideApplicationInsightsOptions();

        foreach (var kind in Enum.GetValues(typeof(ErrorKind)).Cast<ErrorKind>())
        {
            Assert.Equal(
                DomainErrorSeverityMap.Library(kind),
                openTelemetry.SeverityFor(kind));
            Assert.Equal(
                DomainErrorSeverityMap.Library(kind),
                applicationInsights.SeverityFor(kind));
        }
    }

    [Fact]
    public void Operations_raises_client_errors_to_warning_and_drops_unexpected_to_error()
    {
        Assert.Equal(DomainErrorSeverity.Warning, DomainErrorSeverityMap.Operations(ErrorKind.NotFound));
        Assert.Equal(DomainErrorSeverity.Warning, DomainErrorSeverityMap.Operations(ErrorKind.Validation));
        Assert.Equal(DomainErrorSeverity.Warning, DomainErrorSeverityMap.Operations(ErrorKind.BadRequest));
        Assert.Equal(DomainErrorSeverity.Error, DomainErrorSeverityMap.Operations(ErrorKind.Unexpected));
        Assert.Equal(DomainErrorSeverity.Error, DomainErrorSeverityMap.Operations(ErrorKind.ServiceUnavailable));
        Assert.Equal(DomainErrorSeverity.Critical, DomainErrorSeverityMap.Library(ErrorKind.Unexpected));
        Assert.Equal(DomainErrorSeverity.Information, DomainErrorSeverityMap.Library(ErrorKind.NotFound));
    }
}
