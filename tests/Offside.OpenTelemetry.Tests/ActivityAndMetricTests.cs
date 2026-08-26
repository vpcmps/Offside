using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Offside.OpenTelemetry.Tests;

public class ActivityAndMetricTests
{
    [Fact]
    public void An_event_is_added_to_the_activity_in_scope()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.NotFound("order", 42));

        var recorded = harness.SingleEvent();
        Assert.Equal(OffsideTelemetry.ErrorEventName, recorded.Name);
        Assert.Equal("NotFound", Tag(recorded, "offside.kind"));
        Assert.Equal("not_found", Tag(recorded, "offside.code"));
    }

    [Fact]
    public void With_no_activity_in_scope_the_other_signals_still_arrive()
    {
        using var harness = TelemetryHarness.Create(withActivity: false);

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Null(Activity.Current);
        Assert.Single(harness.Logs.Entries);
        Assert.Single(harness.Measurements);
    }

    [Fact]
    public void The_activity_is_left_unset_by_default()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.Unexpected());

        Assert.Equal(ActivityStatusCode.Unset, harness.Activity!.Status);
    }

    [Fact]
    public void The_activity_fails_only_from_the_configured_severity_up()
    {
        using var below = TelemetryHarness.Create(options => options.SetActivityStatusOnError = true);
        below.Recorder.Record(Error.NotFound("order", 42));
        Assert.Equal(ActivityStatusCode.Unset, below.Activity!.Status);

        using var above = TelemetryHarness.Create(options => options.SetActivityStatusOnError = true);
        above.Recorder.Record(Error.Unexpected());
        Assert.Equal(ActivityStatusCode.Error, above.Activity!.Status);
    }

    [Fact]
    public void ServerErrors_marks_only_5xx_kinds()
    {
        using var notFound = TelemetryHarness.Create(options =>
            options.ActivityFailure = ActivityFailurePolicy.ServerErrors);
        notFound.Recorder.Record(Error.NotFound("order", 42));
        Assert.Equal(ActivityStatusCode.Unset, notFound.Activity!.Status);

        using var outage = TelemetryHarness.Create(options =>
            options.ActivityFailure = ActivityFailurePolicy.ServerErrors);
        outage.Recorder.Record(Error.ServiceUnavailable("otp"));
        Assert.Equal(ActivityStatusCode.Error, outage.Activity!.Status);
    }

    [Fact]
    public void The_counter_is_incremented_once_per_error()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.NotFound("order", 42));

        var measurement = harness.SingleMeasurement();
        Assert.Equal(OffsideTelemetry.ErrorCounterName, measurement.Instrument);
        Assert.Equal(1, measurement.Value);
    }

    [Fact]
    public void The_counter_carries_only_low_cardinality_tags()
    {
        using var harness = TelemetryHarness.Create(options => options.IncludeArguments = true);

        harness.Recorder.Record(
            Error.Validation("email", "invalid"),
            new Dictionary<string, string> { ["tenant"] = "acme" });

        var tags = harness.SingleMeasurement().Tags;
        Assert.Equal(2, tags.Length);
        Assert.Contains(tags, tag => tag.Key == "offside.kind");
        Assert.Contains(tags, tag => tag.Key == "offside.code");
        Assert.DoesNotContain(tags, tag => tag.Key is "offside.field" or "tenant");
        Assert.DoesNotContain(tags, tag => tag.Key.StartsWith("offside.arg.", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void Each_signal_can_be_switched_off_on_its_own(bool log, bool activityEvent, bool metric)
    {
        using var harness = TelemetryHarness.Create(options =>
        {
            options.EmitLog = log;
            options.EmitActivityEvent = activityEvent;
            options.EmitMetric = metric;
        });

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal(log ? 1 : 0, harness.Logs.Entries.Count);
        Assert.Equal(activityEvent ? 1 : 0, harness.Activity!.Events.Count());
        Assert.Equal(metric ? 1 : 0, harness.Measurements.Count);
    }

    [Fact]
    public void A_meter_with_no_listener_is_warned_once()
    {
        using var harness = TelemetryHarness.Create(withMeterListener: false);

        harness.Recorder.Record(Error.NotFound("order", 42));
        harness.Recorder.Record(Error.NotFound("order", 43));

        var warnings = harness.Logs.Entries.Where(entry => entry.Level == LogLevel.Warning).ToArray();
        var warning = Assert.Single(warnings);
        Assert.Contains("AddMeter", warning.Message, StringComparison.Ordinal);
        Assert.Contains(OffsideTelemetry.MeterName, warning.Message, StringComparison.Ordinal);
    }

    private static string? Tag(ActivityEvent recorded, string key) =>
        recorded.Tags.FirstOrDefault(tag => tag.Key == key).Value as string;
}
