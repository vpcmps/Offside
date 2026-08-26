using System.Globalization;
using System.Text;

namespace Offside.Testing;

/// <summary>
/// The assertion logic shared by <see cref="Result"/> and <see cref="Result{T}"/>. Both
/// extension surfaces are thin wrappers over these methods, so the rules and the wording of
/// the failure messages exist in exactly one place.
/// </summary>
internal static class AssertionEngine
{
    /// <summary>Asserts that the subject succeeded.</summary>
    /// <param name="isSuccess">Whether the subject succeeded.</param>
    /// <param name="errors">The errors carried by the subject.</param>
    public static void ShouldBeSuccess(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess)
            return;

        throw Fail("Expected the result to be a success, but " + ErrorFormatter.DescribeFailure(errors));
    }

    /// <summary>Asserts that the subject failed.</summary>
    /// <param name="isFailure">Whether the subject failed.</param>
    /// <param name="valueDescription">A rendering of the success value, when the subject carries one.</param>
    public static void ShouldBeFailure(bool isFailure, string? valueDescription)
    {
        if (isFailure)
            return;

        var message = "Expected the result to be a failure, but it succeeded";
        message += valueDescription is null ? "." : " with value " + valueDescription + ".";
        throw Fail(message);
    }

    /// <summary>Finds an error by code, failing when the subject succeeded or has no such error.</summary>
    /// <param name="isFailure">Whether the subject failed.</param>
    /// <param name="errors">The errors carried by the subject.</param>
    /// <param name="code">The expected <see cref="Error.Code"/>.</param>
    /// <returns>The first error carrying <paramref name="code"/>.</returns>
    public static Error FindError(bool isFailure, IReadOnlyList<Error> errors, string code)
    {
        if (code is null)
            throw new ArgumentNullException(nameof(code));

        var expectation = "Expected the result to contain an error with code \"" + code + "\", but ";

        if (!isFailure)
            throw Fail(expectation + "it succeeded.");

        foreach (var error in errors)
        {
            if (string.Equals(error.Code, code, StringComparison.Ordinal))
                return error;
        }

        throw Fail(expectation + ErrorFormatter.DescribeFailure(errors));
    }

    /// <summary>Finds the single error of the subject, failing when there is more than one.</summary>
    /// <param name="isFailure">Whether the subject failed.</param>
    /// <param name="errors">The errors carried by the subject.</param>
    /// <param name="code">The expected <see cref="Error.Code"/>.</param>
    /// <returns>The only error carried by the subject.</returns>
    public static Error FindOnlyError(bool isFailure, IReadOnlyList<Error> errors, string code)
    {
        if (code is null)
            throw new ArgumentNullException(nameof(code));

        var expectation = "Expected the result to contain exactly one error, with code \"" + code + "\", but ";

        if (!isFailure)
            throw Fail(expectation + "it succeeded.");

        if (errors.Count != 1)
            throw Fail(expectation + ErrorFormatter.DescribeFailure(errors));

        var only = errors[0];
        if (!string.Equals(only.Code, code, StringComparison.Ordinal))
            throw Fail(expectation + ErrorFormatter.DescribeFailure(errors));

        return only;
    }

    /// <summary>Asserts that the subject carries exactly these codes, in this order.</summary>
    /// <param name="isFailure">Whether the subject failed.</param>
    /// <param name="errors">The errors carried by the subject.</param>
    /// <param name="codes">The expected codes, in order.</param>
    public static void ShouldHaveErrorsInOrder(bool isFailure, IReadOnlyList<Error> errors, string[] codes)
    {
        if (codes is null)
            throw new ArgumentNullException(nameof(codes));
        if (codes.Length == 0)
            throw new ArgumentException("Expected at least one code.", nameof(codes));

        var expectation = "Expected the result to carry the errors " + DescribeCodes(codes) + " in this order, but ";

        if (!isFailure)
            throw Fail(expectation + "it succeeded.");

        var matches = errors.Count == codes.Length;
        for (var index = 0; matches && index < codes.Length; index++)
            matches = string.Equals(errors[index].Code, codes[index], StringComparison.Ordinal);

        if (!matches)
            throw Fail(expectation + ErrorFormatter.DescribeFailure(errors));
    }

    /// <summary>Asserts the number of errors carried by the subject.</summary>
    /// <param name="isFailure">Whether the subject failed.</param>
    /// <param name="errors">The errors carried by the subject.</param>
    /// <param name="count">The expected number of errors.</param>
    public static void ShouldHaveErrorCount(bool isFailure, IReadOnlyList<Error> errors, int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), count, "A failure carries at least one error.");

        var expected = count.ToString(CultureInfo.InvariantCulture);
        var expectation = "Expected the result to carry " + expected + " error(s), but ";

        if (!isFailure)
            throw Fail(expectation + "it succeeded.");

        if (errors.Count != count)
            throw Fail(expectation + ErrorFormatter.DescribeFailure(errors));
    }

    /// <summary>Builds the exception without throwing it, so call sites can use <c>throw</c>.</summary>
    public static OffsideAssertionException Fail(string message) => new OffsideAssertionException(message);

    private static string DescribeCodes(string[] codes)
    {
        var text = new StringBuilder();
        text.Append('[');

        for (var index = 0; index < codes.Length; index++)
        {
            if (index > 0)
                text.Append(", ");

            text.Append('"').Append(codes[index]).Append('"');
        }

        text.Append(']');
        return text.ToString();
    }
}
