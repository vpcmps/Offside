using Offside.Testing;
using Xunit;

namespace Offside.Testing.Tests;

/// <summary>
/// Covers the assertions over <see cref="Result"/>.
/// </summary>
/// <remarks>
/// These tests use raw xUnit on purpose. Asserting the package with the package would let a
/// "never fails" defect pass unnoticed, so every negative case checks the message text — the
/// diagnostics are the product here, not a side effect.
/// </remarks>
public class ResultAssertionTests
{
    [Fact]
    public void ShouldBeSuccess_passes_for_a_success()
    {
        Result.Success().ShouldBeSuccess();
    }

    [Fact]
    public void ShouldBeSuccess_reports_the_errors_of_a_failure()
    {
        var result = Result.Failure(Error.NotFound("order", 42));

        var exception = Assert.Throws<OffsideAssertionException>(() => result.ShouldBeSuccess());

        Assert.Contains("Expected the result to be a success", exception.Message, StringComparison.Ordinal);
        Assert.Contains("it failed with 1 error(s)", exception.Message, StringComparison.Ordinal);
        Assert.Contains("\"not_found\"", exception.Message, StringComparison.Ordinal);
        Assert.Contains("kind: NotFound", exception.Message, StringComparison.Ordinal);
        Assert.Contains("resource=\"order\"", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldBeFailure_reports_a_success()
    {
        var exception = Assert.Throws<OffsideAssertionException>(() => Result.Success().ShouldBeFailure());

        Assert.Equal("Expected the result to be a failure, but it succeeded.", exception.Message);
    }

    [Fact]
    public void ShouldHaveError_finds_an_error_among_others()
    {
        var result = Result.Failure(Error.NotFound("order", 42), Error.Conflict("order", "already shipped"));

        var assertion = result.ShouldHaveError("conflict");

        Assert.Equal("conflict", assertion.Subject.Code);
    }

    [Fact]
    public void ShouldHaveError_lists_the_actual_errors_when_the_code_is_absent()
    {
        var result = Result.Failure(Error.NotFound("order", 42));

        var exception = Assert.Throws<OffsideAssertionException>(() => result.ShouldHaveError("order.duplicated"));

        Assert.Contains("contain an error with code \"order.duplicated\"", exception.Message, StringComparison.Ordinal);
        Assert.Contains("[0] \"not_found\"", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldHaveError_says_the_result_succeeded_rather_than_listing_nothing()
    {
        var exception = Assert.Throws<OffsideAssertionException>(() => Result.Success().ShouldHaveError("not_found"));

        Assert.Equal(
            "Expected the result to contain an error with code \"not_found\", but it succeeded.",
            exception.Message);
    }

    [Fact]
    public void ShouldHaveOnlyError_passes_for_a_single_matching_error()
    {
        Result.Failure(Error.NotFound("order", 42)).ShouldHaveOnlyError("not_found");
    }

    [Fact]
    public void ShouldHaveOnlyError_names_the_extra_error()
    {
        var result = Result.Failure(Error.NotFound("order", 42), Error.Conflict("order"));

        var exception = Assert.Throws<OffsideAssertionException>(() => result.ShouldHaveOnlyError("not_found"));

        Assert.Contains("exactly one error, with code \"not_found\"", exception.Message, StringComparison.Ordinal);
        Assert.Contains("it failed with 2 error(s)", exception.Message, StringComparison.Ordinal);
        Assert.Contains("[1] \"conflict\"", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldHaveErrorsInOrder_passes_for_the_exact_sequence()
    {
        var result = Result.Combine(
            Result.Failure(Error.Validation("email")),
            Result.Failure(Error.Validation("age", "age.invalid")));

        result.ShouldHaveErrorsInOrder("validation", "age.invalid");
    }

    [Fact]
    public void ShouldHaveErrorsInOrder_reports_the_actual_sequence()
    {
        var result = Result.Failure(Error.Validation("age", "age.invalid"), Error.Validation("email"));

        var exception = Assert.Throws<OffsideAssertionException>(
            () => result.ShouldHaveErrorsInOrder("validation", "age.invalid"));

        Assert.Contains("[\"validation\", \"age.invalid\"] in this order", exception.Message, StringComparison.Ordinal);
        Assert.Contains("[0] \"age.invalid\"", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldHaveErrorsInOrder_rejects_an_empty_expectation()
    {
        var result = Result.Failure(Error.NotFound("order"));

        Assert.Throws<ArgumentException>(() => result.ShouldHaveErrorsInOrder());
    }

    [Fact]
    public void ShouldHaveErrorCount_reports_the_actual_count()
    {
        var result = Result.Failure(Error.NotFound("order"), Error.Conflict("order"));

        var exception = Assert.Throws<OffsideAssertionException>(() => result.ShouldHaveErrorCount(1));

        Assert.Contains("carry 1 error(s)", exception.Message, StringComparison.Ordinal);
        Assert.Contains("it failed with 2 error(s)", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Assertions_chain_through_And_and_stand_alone()
    {
        var result = Result.Failure(Error.Validation("email"), Error.Conflict("order"));

        result.ShouldHaveError("validation").ForField("email")
            .And.ShouldHaveError("conflict").WithKind(ErrorKind.Conflict);

        result.ShouldHaveErrorCount(2);
    }
}
