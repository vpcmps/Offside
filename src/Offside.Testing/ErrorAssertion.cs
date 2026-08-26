using System.Globalization;

namespace Offside.Testing;

/// <summary>
/// Refines assertions about a single <see cref="Error"/> that was already located on a result.
/// </summary>
/// <typeparam name="TResult">
/// The type of the result the error came from — <see cref="Result"/> or <see cref="Result{T}"/>.
/// It exists only so <see cref="And"/> can hand the original result back for further assertions.
/// </typeparam>
/// <remarks>
/// Because the error is fixed before any refinement runs, a failure here can say exactly which
/// part of the error disagreed, rather than "no error matched".
/// </remarks>
public sealed class ErrorAssertion<TResult>
{
    private readonly TResult _result;

    internal ErrorAssertion(TResult result, Error error)
    {
        _result = result;
        Subject = error;
    }

    /// <summary>Gets the error these assertions are about.</summary>
    public Error Subject { get; }

    /// <summary>
    /// Gets the result the error came from, so another assertion can be chained onto it.
    /// Using it is optional — starting a new statement on the same result works identically.
    /// </summary>
    public TResult And => _result;

    /// <summary>Asserts the error's <see cref="Error.Kind"/>.</summary>
    /// <param name="kind">The expected kind.</param>
    /// <returns>This instance, for chaining.</returns>
    public ErrorAssertion<TResult> WithKind(ErrorKind kind)
    {
        if (Subject.Kind != kind)
            throw Fail("kind " + kind, "kind " + Subject.Kind);

        return this;
    }

    /// <summary>Asserts the error's <see cref="Error.ErrorCode"/>.</summary>
    /// <param name="errorCode">The expected error code.</param>
    /// <returns>This instance, for chaining.</returns>
    public ErrorAssertion<TResult> WithErrorCode(string errorCode)
    {
        if (errorCode is null)
            throw new ArgumentNullException(nameof(errorCode));

        if (!string.Equals(Subject.ErrorCode, errorCode, StringComparison.Ordinal))
            throw Fail("errorCode \"" + errorCode + "\"", "errorCode \"" + Subject.ErrorCode + "\"");

        return this;
    }

    /// <summary>Asserts the error's <see cref="Error.Field"/>.</summary>
    /// <param name="field">The expected field, or <see langword="null"/> to assert the error has none.</param>
    /// <returns>This instance, for chaining.</returns>
    public ErrorAssertion<TResult> ForField(string? field)
    {
        if (!string.Equals(Subject.Field, field, StringComparison.Ordinal))
            throw Fail("field " + Describe(field), "field " + Describe(Subject.Field));

        return this;
    }

    /// <summary>Asserts that the error carries an argument with this name and value.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The expected value.</param>
    /// <returns>This instance, for chaining.</returns>
    public ErrorAssertion<TResult> WithArgument(string name, object? value)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        var expected = "argument " + name + "=" + ErrorFormatter.DescribeValue(value);

        if (!Subject.Arguments.TryGetValue(name, out var actual))
            throw Fail(expected, "arguments " + ErrorFormatter.DescribeArguments(Subject.Arguments));

        if (!Equals(actual, value))
            throw Fail(expected, "argument " + name + "=" + ErrorFormatter.DescribeValue(actual));

        return this;
    }

    /// <summary>
    /// Asserts the message the error resolves to, using <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    /// <param name="resolver">The resolver to render the message with.</param>
    /// <param name="message">The expected message, after interpolation.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <remarks>
    /// The built-in resolver returns <see cref="Error.Code"/> when the catalog has no entry for
    /// it, so a passing assertion here does not prove the catalog defines the code. Use
    /// <see cref="OffsideCatalog"/> for that.
    /// </remarks>
    public ErrorAssertion<TResult> WithMessage(IErrorMessageResolver resolver, string message) =>
        WithMessage(resolver, CultureInfo.InvariantCulture, message);

    /// <summary>Asserts the message the error resolves to in a given culture.</summary>
    /// <param name="resolver">The resolver to render the message with.</param>
    /// <param name="culture">The culture to resolve in.</param>
    /// <param name="message">The expected message, after interpolation.</param>
    /// <returns>This instance, for chaining.</returns>
    public ErrorAssertion<TResult> WithMessage(IErrorMessageResolver resolver, CultureInfo culture, string message)
    {
        if (resolver is null)
            throw new ArgumentNullException(nameof(resolver));
        if (culture is null)
            throw new ArgumentNullException(nameof(culture));
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var actual = resolver.GetMessage(Subject, culture);
        if (!string.Equals(actual, message, StringComparison.Ordinal))
            throw Fail("message \"" + message + "\"", "message \"" + actual + "\"");

        return this;
    }

    private static string Describe(string? field) => field is null ? "null" : "\"" + field + "\"";

    private OffsideAssertionException Fail(string expected, string actual) =>
        AssertionEngine.Fail(
            "Expected error " + ErrorFormatter.Describe(Subject) + Environment.NewLine +
            "to have " + expected + ", but found " + actual + ".");
}
