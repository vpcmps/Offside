using Offside;
using Offside.Testing;
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
    public void Failure_snapshots_errors()
    {
        var errors = new List<Error> { Error.NotFound("order", 1) };

        var result = Result.Failure(errors);
        var resultT = Result<int>.Failure(errors);
        errors.Clear();

        Assert.Equal(Error.NotFound("order", 1), Assert.Single(result.Errors));
        Assert.Equal(Error.NotFound("order", 1), Assert.Single(resultT.Errors));
    }

    [Fact]
    public void Default_result_is_success()
    {
        var result = default(Result);

        result.ShouldBeSuccess();
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Default_result_t_is_success()
    {
        var result = default(Result<int>);

        result.ShouldBeSuccess();
        Assert.Equal(0, result.Value);
        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void Value_on_failure_throws()
    {
        var result = Result<int>.Failure(Error.NotFound("order", 1));

        result.ShouldBeFailure();
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

        result.ShouldBeSuccess();
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

    [Fact]
    public void Non_generic_match_branches()
    {
        var ok = Result.Success().Match(() => 1, _ => -1);
        var fail = Result.Failure(Error.BadRequest()).Match(() => 1, _ => -1);

        Assert.Equal(1, ok);
        Assert.Equal(-1, fail);
    }
}
