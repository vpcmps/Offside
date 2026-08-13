using Offside;
using Xunit;

namespace Offside.Tests;

public sealed class ResultCombinatorTests
{
    [Fact]
    public void Bind_short_circuits_on_failure()
    {
        var called = false;
        var result = Result<int>.Failure(Error.NotFound("order", 1))
            .Bind(_ =>
            {
                called = true;
                return Result<string>.Success("x");
            });

        Assert.True(result.IsFailure);
        Assert.False(called);
        Assert.Equal("not_found", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Map_transforms_success()
    {
        var result = Result<int>.Success(2).Map(x => x * 3);

        Assert.Equal(6, result.Value);
    }

    [Fact]
    public void Combine_merges_failures()
    {
        var left = Result<int>.Failure(Error.Validation("email"));
        var right = Result<int>.Failure(Error.Validation("name"));

        var combined = Result.Combine(left, right);

        Assert.True(combined.IsFailure);
        Assert.Equal(2, combined.Errors.Count);
        Assert.Equal("email", combined.Errors[0].Field);
        Assert.Equal("name", combined.Errors[1].Field);
    }

    [Fact]
    public void Combine_succeeds_when_all_succeed()
    {
        var combined = Result.Combine(
            Result<int>.Success(1),
            Result<int>.Success(2));

        Assert.True(combined.IsSuccess);
    }
}
