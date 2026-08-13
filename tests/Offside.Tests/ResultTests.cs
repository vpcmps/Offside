using Offside;
using Xunit;

namespace Offside.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Failure_without_errors_throws()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure());
        Assert.Throws<ArgumentException>(() => Result<int>.Failure());
    }

    [Fact]
    public void Value_on_failure_throws()
    {
        var result = Result<int>.Failure(Error.NotFound("order", 1));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void TryGetValue_returns_false_on_failure()
    {
        var result = Result<int>.Failure(Error.NotFound("order", 1));

        Assert.False(result.TryGetValue(out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void Success_exposes_value()
    {
        var result = Result<int>.Success(7);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(7, value);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void Match_branches()
    {
        var ok = Result<int>.Success(3).Match(v => v * 2, _ => -1);
        var fail = Result<int>.Failure(Error.BadRequest()).Match(v => v, _ => -1);

        Assert.Equal(6, ok);
        Assert.Equal(-1, fail);
    }
}
