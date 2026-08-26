using Xunit;

namespace Offside.OpenTelemetry.Tests;

public class ResultTelemetryExtensionsTests
{
    [Fact]
    public void A_failed_result_records_one_entry_per_error_in_order()
    {
        using var harness = TelemetryHarness.Create();
        var result = Result.Failure(Error.NotFound("order", 42), Error.Validation("email", "invalid"));

        var returned = result.RecordTo(harness.Recorder);

        Assert.Equal(result.Errors, returned.Errors);
        Assert.Collection(
            harness.Logs.Entries,
            first => Assert.Equal("not_found", first.Dimension("offside.code")),
            second => Assert.Equal("invalid", second.Dimension("offside.code")));
    }

    [Fact]
    public void A_successful_result_records_nothing()
    {
        using var harness = TelemetryHarness.Create();

        Result.Success().RecordTo(harness.Recorder);
        Result<int>.Success(1).RecordTo(harness.Recorder);

        Assert.Empty(harness.Logs.Entries);
    }

    [Fact]
    public void Extra_dimensions_reach_every_entry()
    {
        using var harness = TelemetryHarness.Create();
        var properties = new Dictionary<string, string> { ["tenant"] = "acme" };

        Result<int>.Failure(Error.NotFound("order", 42), Error.Unexpected())
            .RecordTo(harness.Recorder, properties);

        Assert.All(harness.Logs.Entries, entry => Assert.Equal("acme", entry.Dimension("tenant")));
    }

    [Fact]
    public void A_null_recorder_is_a_programming_error()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success().RecordTo(null!));
        Assert.Throws<ArgumentNullException>(() => Result<int>.Success(1).RecordTo(null!));
    }
}
