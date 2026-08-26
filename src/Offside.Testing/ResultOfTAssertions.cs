namespace Offside.Testing;

/// <summary>
/// Fluent assertions for <see cref="Result{T}"/>. Mirrors <see cref="ResultAssertions"/>, plus
/// the value refinements a successful result makes possible.
/// </summary>
public static class ResultOfTAssertions
{
    /// <summary>Asserts that the result succeeded, and exposes its value for refinement.</summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="result">The result under test.</param>
    /// <returns>An assertion over the success value.</returns>
    /// <exception cref="OffsideAssertionException">The result failed. The message lists its errors.</exception>
    public static SuccessAssertion<T> ShouldBeSuccess<T>(this Result<T> result)
    {
        AssertionEngine.ShouldBeSuccess(result.IsSuccess, result.Errors);
        return new SuccessAssertion<T>(result, result.Value);
    }

    /// <summary>Asserts that the result failed, without saying how.</summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="result">The result under test.</param>
    /// <returns>The same result, so further assertions can be chained.</returns>
    /// <exception cref="OffsideAssertionException">The result succeeded. The message names the value.</exception>
    public static Result<T> ShouldBeFailure<T>(this Result<T> result)
    {
        var valueDescription = result.TryGetValue(out var value)
            ? ErrorFormatter.DescribeValue(value)
            : null;

        AssertionEngine.ShouldBeFailure(result.IsFailure, valueDescription);
        return result;
    }

    /// <summary>
    /// Asserts that the result failed carrying an error with this code, ignoring any other error.
    /// </summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="result">The result under test.</param>
    /// <param name="code">The expected <see cref="Error.Code"/>.</param>
    /// <returns>An assertion over the matched error.</returns>
    /// <exception cref="OffsideAssertionException">The result succeeded or carries no such error.</exception>
    public static ErrorAssertion<Result<T>> ShouldHaveError<T>(this Result<T> result, string code) =>
        new ErrorAssertion<Result<T>>(result, AssertionEngine.FindError(result.IsFailure, result.Errors, code));

    /// <summary>Asserts that the result failed carrying this error and nothing else.</summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="result">The result under test.</param>
    /// <param name="code">The expected <see cref="Error.Code"/>.</param>
    /// <returns>An assertion over the single error.</returns>
    /// <exception cref="OffsideAssertionException">The result succeeded, carries another error, or carries more than one.</exception>
    public static ErrorAssertion<Result<T>> ShouldHaveOnlyError<T>(this Result<T> result, string code) =>
        new ErrorAssertion<Result<T>>(result, AssertionEngine.FindOnlyError(result.IsFailure, result.Errors, code));

    /// <summary>Asserts the exact sequence of error codes carried by the result.</summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="result">The result under test.</param>
    /// <param name="codes">The expected codes, in order.</param>
    /// <returns>The same result, so further assertions can be chained.</returns>
    /// <remarks>See the remarks on <see cref="ResultAssertions.ShouldHaveErrorsInOrder"/> before pinning down order.</remarks>
    /// <exception cref="OffsideAssertionException">The result succeeded or carries a different sequence.</exception>
    public static Result<T> ShouldHaveErrorsInOrder<T>(this Result<T> result, params string[] codes)
    {
        AssertionEngine.ShouldHaveErrorsInOrder(result.IsFailure, result.Errors, codes);
        return result;
    }

    /// <summary>Asserts how many errors the result carries, without saying which.</summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="result">The result under test.</param>
    /// <param name="count">The expected number of errors.</param>
    /// <returns>The same result, so further assertions can be chained.</returns>
    /// <exception cref="OffsideAssertionException">The result succeeded or carries a different number of errors.</exception>
    public static Result<T> ShouldHaveErrorCount<T>(this Result<T> result, int count)
    {
        AssertionEngine.ShouldHaveErrorCount(result.IsFailure, result.Errors, count);
        return result;
    }
}
