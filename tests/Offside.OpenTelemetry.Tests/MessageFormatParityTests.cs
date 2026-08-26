using System.Reflection;
using Xunit;

using ApplicationInsightsFormat = Offside.ApplicationInsights.DomainErrorMessageFormat;

namespace Offside.OpenTelemetry.Tests;

/// <summary>
/// The two integrations carry their own copy of the message formats, since neither package
/// references the other. This is what stops the copies drifting: a host that swaps the classic
/// Application Insights SDK for OpenTelemetry must not see its log lines change shape.
/// </summary>
public class MessageFormatParityTests
{
    public static TheoryData<string> FormatNames() => new()
    {
        nameof(DomainErrorMessageFormat.MessageOnly),
        nameof(DomainErrorMessageFormat.CodePrefixed),
        nameof(DomainErrorMessageFormat.ErrorCodePrefixed)
    };

    [Theory]
    [MemberData(nameof(FormatNames))]
    public void Both_integrations_render_a_format_the_same_way(string name)
    {
        var error = Error.Conflict("order", "already shipped");
        const string message = "Order already shipped.";

        Assert.Equal(
            Format(typeof(ApplicationInsightsFormat), name)(error, message),
            Format(typeof(DomainErrorMessageFormat), name)(error, message));
    }

    [Fact]
    public void Both_integrations_offer_the_same_set_of_formats()
    {
        Assert.Equal(FormatNamesOf(typeof(ApplicationInsightsFormat)), FormatNamesOf(typeof(DomainErrorMessageFormat)));
    }

    [Fact]
    public void Both_integrations_default_to_the_same_format()
    {
        var openTelemetry = new OffsideOpenTelemetryOptions();
        var applicationInsights = new Offside.ApplicationInsights.OffsideApplicationInsightsOptions();
        var error = Error.Conflict("order", "already shipped");

        Assert.Equal(
            applicationInsights.FormatMessage(error, "Order already shipped."),
            openTelemetry.FormatMessage(error, "Order already shipped."));
    }

    private static Func<Error, string, string> Format(Type holder, string name) =>
        (Func<Error, string, string>)holder.GetField(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    private static IEnumerable<string> FormatNamesOf(Type holder) =>
        holder.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => field.Name)
            .OrderBy(name => name, StringComparer.Ordinal);
}
