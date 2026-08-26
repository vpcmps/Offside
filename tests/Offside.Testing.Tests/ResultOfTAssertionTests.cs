using Offside.Testing;
using Xunit;

namespace Offside.Testing.Tests;

/// <summary>Covers the assertions over <see cref="Result{T}"/>, including value refinement.</summary>
public class ResultOfTAssertionTests
{
    private sealed record Order(int Id, string Status);

    [Fact]
    public void ShouldBeSuccess_exposes_the_value()
    {
        var result = Result<Order>.Success(new Order(42, "paid"));

        var order = result.ShouldBeSuccess().Subject;

        Assert.Equal(42, order.Id);
    }

    [Fact]
    public void WithValue_compares_by_equality()
    {
        var result = Result<Order>.Success(new Order(42, "paid"));

        result.ShouldBeSuccess().WithValue(new Order(42, "paid"));

        var exception = Assert.Throws<OffsideAssertionException>(
            () => result.ShouldBeSuccess().WithValue(new Order(42, "shipped")));

        Assert.Contains("Expected the result value to be Order { Id = 42, Status = shipped }", exception.Message, StringComparison.Ordinal);
        Assert.Contains("but found Order { Id = 42, Status = paid }.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithValue_predicate_uses_the_description_when_given()
    {
        var result = Result<Order>.Success(new Order(42, "paid"));

        result.ShouldBeSuccess().WithValue(order => order.Status == "paid");

        var described = Assert.Throws<OffsideAssertionException>(
            () => result.ShouldBeSuccess().WithValue(order => order.Status == "shipped", "shipped"));
        Assert.Contains("Expected the result value to be shipped, but found", described.Message, StringComparison.Ordinal);

        var undescribed = Assert.Throws<OffsideAssertionException>(
            () => result.ShouldBeSuccess().WithValue(order => order.Id == 7));
        Assert.Contains("Expected the result value to satisfy the predicate, but found", undescribed.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldBeFailure_names_the_value_it_found()
    {
        var result = Result<Order>.Success(new Order(42, "paid"));

        var exception = Assert.Throws<OffsideAssertionException>(() => result.ShouldBeFailure());

        Assert.Contains("but it succeeded with value Order { Id = 42, Status = paid }.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldBeSuccess_reports_the_errors_of_a_failure()
    {
        var result = Result<Order>.Failure(Error.NotFound("order", 42));

        var exception = Assert.Throws<OffsideAssertionException>(() => result.ShouldBeSuccess());

        Assert.Contains("it failed with 1 error(s)", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_assertions_mirror_the_non_generic_surface()
    {
        var result = Result<Order>.Failure(Error.Validation("email"), Error.Conflict("order"));

        result.ShouldHaveError("validation").ForField("email")
            .And.ShouldHaveErrorCount(2);

        result.ShouldHaveErrorsInOrder("validation", "conflict");
        Assert.Equal("conflict", result.ShouldHaveError("conflict").Subject.Code);
    }

    [Fact]
    public void ShouldHaveOnlyError_is_stricter_than_ShouldHaveError()
    {
        var result = Result<Order>.Failure(Error.Validation("email"), Error.Conflict("order"));

        result.ShouldHaveError("validation");

        Assert.Throws<OffsideAssertionException>(() => result.ShouldHaveOnlyError("validation"));
    }
}
