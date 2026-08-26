using Xunit;

namespace Offside.OpenTelemetry.Tests;

public class MessageFormatParityTests
{
    [Fact]
    public void Both_integrations_default_to_the_same_format()
    {
        var openTelemetry = new OffsideOpenTelemetryOptions();
        var applicationInsights = new Offside.ApplicationInsights.OffsideApplicationInsightsOptions();
        var error = Error.Conflict("order", "already shipped");

        Assert.Equal(
            applicationInsights.FormatMessage(error, "Order already shipped."),
            openTelemetry.FormatMessage(error, "Order already shipped."));
        Assert.Equal(
            DomainErrorMessageFormat.MessageOnly(error, "Order already shipped."),
            openTelemetry.FormatMessage(error, "Order already shipped."));
    }

    [Fact]
    public void Both_integrations_default_to_the_library_severity_map()
    {
        var openTelemetry = new OffsideOpenTelemetryOptions();
        var applicationInsights = new Offside.ApplicationInsights.OffsideApplicationInsightsOptions();

        foreach (var kind in Enum.GetValues(typeof(ErrorKind)).Cast<ErrorKind>())
            Assert.Equal(openTelemetry.SeverityFor(kind), applicationInsights.SeverityFor(kind));
    }
}
