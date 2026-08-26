using Offside.Testing;
using Xunit;

namespace Offside.Tests;

public sealed class RecordToTests
{
    private sealed class RecordingRecorder : IDomainErrorRecorder
    {
        public List<(Error Error, IReadOnlyDictionary<string, string>? Properties)> Entries { get; } = [];

        public void Record(Error error, IReadOnlyDictionary<string, string>? properties = null) =>
            Entries.Add((error, properties));
    }

    [Fact]
    public void A_failure_records_each_error_in_order()
    {
        var recorder = new RecordingRecorder();
        var first = Error.Validation("email");
        var second = Error.NotFound("order", 1);

        var returned = Result.Failure(first, second).RecordTo(recorder);

        returned.ShouldHaveErrorsInOrder("validation", "not_found");
        Assert.Equal(new[] { first, second }, recorder.Entries.Select(entry => entry.Error));
    }

    [Fact]
    public void A_value_failure_records_the_same_way()
    {
        var recorder = new RecordingRecorder();
        var properties = new Dictionary<string, string> { ["tenant"] = "acme" };

        Result<int>.Failure(Error.Conflict("order")).RecordTo(recorder, properties);

        var recorded = Assert.Single(recorder.Entries);
        Assert.Equal("conflict", recorded.Error.Code);
        Assert.Equal("acme", recorded.Properties!["tenant"]);
    }

    [Fact]
    public void A_success_records_nothing()
    {
        var recorder = new RecordingRecorder();

        Result.Success().RecordTo(recorder);
        Result<int>.Success(1).RecordTo(recorder);

        Assert.Empty(recorder.Entries);
    }

    [Fact]
    public void A_null_recorder_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success().RecordTo(null!));
        Assert.Throws<ArgumentNullException>(() => Result<int>.Success(1).RecordTo(null!));
    }
}
