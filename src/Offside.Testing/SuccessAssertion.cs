namespace Offside.Testing;

/// <summary>
/// Refines assertions about the value of a successful <see cref="Result{T}"/>.
/// </summary>
/// <typeparam name="T">The value type of the result.</typeparam>
public sealed class SuccessAssertion<T>
{
    private readonly Result<T> _result;

    internal SuccessAssertion(Result<T> result, T value)
    {
        _result = result;
        Subject = value;
    }

    /// <summary>Gets the value carried by the result, for assertions this package does not cover.</summary>
    public T Subject { get; }

    /// <summary>
    /// Gets the result the value came from, so another assertion can be chained onto it.
    /// Using it is optional — starting a new statement on the same result works identically.
    /// </summary>
    public Result<T> And => _result;

    /// <summary>Asserts the value using its own equality.</summary>
    /// <param name="value">The expected value.</param>
    /// <returns>This instance, for chaining.</returns>
    public SuccessAssertion<T> WithValue(T value)
    {
        if (!EqualityComparer<T>.Default.Equals(Subject, value))
        {
            throw AssertionEngine.Fail(
                "Expected the result value to be " + ErrorFormatter.DescribeValue(value) +
                ", but found " + ErrorFormatter.DescribeValue(Subject) + ".");
        }

        return this;
    }

    /// <summary>Asserts the value against a predicate.</summary>
    /// <param name="predicate">The condition the value must satisfy.</param>
    /// <param name="description">
    /// An optional description of the condition, used in the failure message. Without it the
    /// message can only report the value that was rejected.
    /// </param>
    /// <returns>This instance, for chaining.</returns>
    public SuccessAssertion<T> WithValue(Func<T, bool> predicate, string? description = null)
    {
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));

        if (!predicate(Subject))
        {
            var expectation = description is null ? "to satisfy the predicate" : "to be " + description;
            throw AssertionEngine.Fail(
                "Expected the result value " + expectation +
                ", but found " + ErrorFormatter.DescribeValue(Subject) + ".");
        }

        return this;
    }
}
