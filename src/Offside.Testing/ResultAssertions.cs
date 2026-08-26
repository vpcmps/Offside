namespace Offside.Testing;

/// <summary>
/// Fluent assertions for <see cref="Result"/>.
/// </summary>
/// <remarks>
/// The names are deliberately not <c>Should()</c>, so the package can be used in the same file
/// as FluentAssertions or Shouldly without colliding with their entry point.
/// </remarks>
public static class ResultAssertions
{
    /// <summary>Asserts that the result succeeded.</summary>
    /// <param name="result">The result under test.</param>
    /// <returns>The same result, so further assertions can be chained.</returns>
    /// <exception cref="OffsideAssertionException">The result failed. The message lists its errors.</exception>
    public static Result ShouldBeSuccess(this Result result)
    {
        AssertionEngine.ShouldBeSuccess(result.IsSuccess, result.Errors);
        return result;
    }

    /// <summary>Asserts that the result failed, without saying how.</summary>
    /// <param name="result">The result under test.</param>
    /// <returns>The same result, so further assertions can be chained.</returns>
    /// <exception cref="OffsideAssertionException">The result succeeded.</exception>
    public static Result ShouldBeFailure(this Result result)
    {
        AssertionEngine.ShouldBeFailure(result.IsFailure, valueDescription: null);
        return result;
    }

    /// <summary>
    /// Asserts that the result failed carrying an error with this code, ignoring any other error.
    /// This is the default choice: it survives a rule being added elsewhere in the flow.
    /// </summary>
    /// <param name="result">The result under test.</param>
    /// <param name="code">The expected <see cref="Error.Code"/>.</param>
    /// <returns>An assertion over the matched error.</returns>
    /// <exception cref="OffsideAssertionException">The result succeeded or carries no such error.</exception>
    public static ErrorAssertion<Result> ShouldHaveError(this Result result, string code) =>
        new ErrorAssertion<Result>(result, AssertionEngine.FindError(result.IsFailure, result.Errors, code));

    /// <summary>
    /// Asserts that the result failed carrying this error and nothing else. Stricter than
    /// <see cref="ShouldHaveError(Result, string)"/> — use it to catch an extra error leaking in.
    /// </summary>
    /// <param name="result">The result under test.</param>
    /// <param name="code">The expected <see cref="Error.Code"/>.</param>
    /// <returns>An assertion over the single error.</returns>
    /// <exception cref="OffsideAssertionException">The result succeeded, carries another error, or carries more than one.</exception>
    public static ErrorAssertion<Result> ShouldHaveOnlyError(this Result result, string code) =>
        new ErrorAssertion<Result>(result, AssertionEngine.FindOnlyError(result.IsFailure, result.Errors, code));

    /// <summary>Asserts the exact sequence of error codes carried by the result.</summary>
    /// <param name="result">The result under test.</param>
    /// <param name="codes">The expected codes, in order.</param>
    /// <returns>The same result, so further assertions can be chained.</returns>
    /// <remarks>
    /// Order comes from the source: argument order for <see cref="Result.Combine(Result[])"/>, and
    /// rule declaration order for errors bridged from FluentValidation. Reordering rules will break
    /// this assertion without any behaviour changing, so prefer
    /// <see cref="ShouldHaveError(Result, string)"/> unless the order is what you mean to pin down.
    /// </remarks>
    /// <exception cref="OffsideAssertionException">The result succeeded or carries a different sequence.</exception>
    public static Result ShouldHaveErrorsInOrder(this Result result, params string[] codes)
    {
        AssertionEngine.ShouldHaveErrorsInOrder(result.IsFailure, result.Errors, codes);
        return result;
    }

    /// <summary>Asserts how many errors the result carries, without saying which.</summary>
    /// <param name="result">The result under test.</param>
    /// <param name="count">The expected number of errors.</param>
    /// <returns>The same result, so further assertions can be chained.</returns>
    /// <exception cref="OffsideAssertionException">The result succeeded or carries a different number of errors.</exception>
    public static Result ShouldHaveErrorCount(this Result result, int count)
    {
        AssertionEngine.ShouldHaveErrorCount(result.IsFailure, result.Errors, count);
        return result;
    }
}
