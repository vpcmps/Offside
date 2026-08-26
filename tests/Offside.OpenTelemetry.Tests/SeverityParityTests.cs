using Microsoft.ApplicationInsights.DataContracts;
using Xunit;

namespace Offside.OpenTelemetry.Tests;

/// <summary>
/// The two integrations keep separate severity tables on purpose — neither package references the
/// other. This is what stops them drifting apart: a host that swaps the classic Application
/// Insights SDK for OpenTelemetry must not see its severities change underneath it.
/// </summary>
public class SeverityParityTests
{
    [Fact]
    public void Both_integrations_agree_on_every_kind()
    {
        var openTelemetry = new OffsideOpenTelemetryOptions();
        var applicationInsights = new Offside.ApplicationInsights.OffsideApplicationInsightsOptions();

        foreach (var kind in Enum.GetValues(typeof(ErrorKind)).Cast<ErrorKind>())
        {
            var mine = openTelemetry.SeverityFor(kind);
            var theirs = applicationInsights.SeverityFor(kind);

            Assert.Equal(
                Enum.GetName(typeof(SeverityLevel), theirs),
                Enum.GetName(typeof(DomainErrorSeverity), mine));
        }
    }

    [Fact]
    public void The_severity_scales_line_up_name_for_name()
    {
        Assert.Equal(
            Enum.GetNames(typeof(SeverityLevel)).OrderBy(name => name, StringComparer.Ordinal),
            Enum.GetNames(typeof(DomainErrorSeverity)).OrderBy(name => name, StringComparer.Ordinal));
    }
}
