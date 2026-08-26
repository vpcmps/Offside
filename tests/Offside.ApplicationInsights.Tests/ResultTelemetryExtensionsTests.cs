using Offside.Testing;
using Xunit;

namespace Offside.ApplicationInsights.Tests;

public class ResultTelemetryExtensionsTests
{
    [Fact]
    public void A_failed_result_records_one_trace_per_error_in_order()
    {
        using var harness = TelemetryHarness.Create();
        var result = Result.Failure(Error.NotFound("order", 42), Error.Conflict("order", "already shipped"));

        var returned = result.RecordTo(harness.Recorder);

        Assert.Equal(2, harness.Traces.Count);
        Assert.Equal("not_found", harness.Traces[0].Properties["offside.code"]);
        Assert.Equal("conflict", harness.Traces[1].Properties["offside.code"]);
        returned.ShouldHaveErrorsInOrder("not_found", "conflict");
    }

    [Fact]
    public void A_successful_result_records_nothing()
    {
        using var harness = TelemetryHarness.Create();

        Result.Success().RecordTo(harness.Recorder);
        Result<string>.Success("ok").RecordTo(harness.Recorder);

        Assert.Empty(harness.Traces);
    }

    [Fact]
    public void A_failed_value_result_records_its_errors_and_flows_through()
    {
        using var harness = TelemetryHarness.Create();
        var result = Result<string>.Failure(Error.Unauthorized("token expired"));

        var returned = result.RecordTo(harness.Recorder, new Dictionary<string, string> { ["tenant"] = "acme" });

        Assert.Equal("acme", harness.SingleTrace().Properties["tenant"]);
        returned.ShouldBeFailure().ShouldHaveOnlyError("unauthorized");
    }

    [Fact]
    public void A_null_recorder_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success().RecordTo(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success("ok").RecordTo(null!));
    }
}
