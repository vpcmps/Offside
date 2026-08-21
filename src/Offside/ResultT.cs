namespace Offside;

/// <summary>
/// The outcome of an operation that produces a value: either success carrying a
/// <typeparamref name="T"/>, or failure carrying one or more <see cref="Error"/> instances.
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
/// <remarks>
/// There is deliberately no implicit conversion from <typeparamref name="T"/>: constructing a
/// result is always explicit, so a value never becomes a success by accident.
/// </remarks>
/// <example>
/// <code>
/// public Result&lt;Order&gt; Get(string id)
/// {
///     var order = _orders.Find(id);
///     return order is null
///         ? Result&lt;Order&gt;.Failure(Error.NotFound("order", id))
///         : Result&lt;Order&gt;.Success(order);
/// }
/// </code>
/// </example>
public readonly struct Result<T>
{
    private readonly T _value;
    private readonly IReadOnlyList<Error> _errors;
    private readonly bool _failed;

    private Result(bool failed, T value, IReadOnlyList<Error> errors)
    {
        _failed = failed;
        _value = value;
        _errors = errors;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess => !_failed;

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => _failed;

    /// <summary>Gets the value produced on success.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure. Check <see cref="IsSuccess"/> or use <see cref="TryGetValue"/> first.</exception>
    public T Value => !_failed
        ? _value
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    /// <summary>Gets the errors that caused the failure, or an empty list on success.</summary>
    public IReadOnlyList<Error> Errors => _errors ?? Array.Empty<Error>();

    /// <summary>Reads the value without throwing.</summary>
    /// <param name="value">Receives the value on success, or <c>default</c> on failure.</param>
    /// <returns><see langword="true"/> when the result is a success.</returns>
    public bool TryGetValue(out T value)
    {
        if (!_failed)
        {
            value = _value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>Runs one of two functions depending on the outcome.</summary>
    /// <typeparam name="TOut">The type both branches return.</typeparam>
    /// <param name="onSuccess">Invoked with the value when the result is a success.</param>
    /// <param name="onFailure">Invoked with the errors when the result is a failure.</param>
    /// <returns>The value produced by the branch that ran.</returns>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        !_failed ? onSuccess(_value) : onFailure(Errors);

    /// <summary>Transforms the value of a successful result, leaving a failure untouched.</summary>
    /// <typeparam name="TOut">The transformed value type.</typeparam>
    /// <param name="map">Applied to the value. Not invoked when the result is a failure.</param>
    /// <returns>A result carrying the transformed value, or the original errors.</returns>
    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        !_failed ? Result<TOut>.Success(map(Value)) : Result<TOut>.Failure(Errors);

    /// <summary>Chains another operation that can itself fail, short-circuiting on failure.</summary>
    /// <typeparam name="TOut">The value type of the next operation.</typeparam>
    /// <param name="bind">Applied to the value. Not invoked when the result is a failure.</param>
    /// <returns>The result of <paramref name="bind"/>, or a failure carrying the original errors.</returns>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind) =>
        !_failed ? bind(Value) : Result<TOut>.Failure(Errors);

    /// <summary>Creates a successful result.</summary>
    /// <param name="value">The value produced.</param>
    /// <returns>The result.</returns>
    public static Result<T> Success(T value) => new Result<T>(false, value, Array.Empty<Error>());

    /// <summary>Creates a failed result.</summary>
    /// <param name="errors">The errors, in the order they should be reported. At least one is required.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Result<T> Failure(params Error[] errors) => Failure((IEnumerable<Error>)errors);

    /// <summary>Creates a failed result from a sequence, which is copied immediately.</summary>
    /// <param name="errors">The errors, in the order they should be reported. At least one is required.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Result<T> Failure(IEnumerable<Error> errors) =>
        new Result<T>(true, default!, Result.SnapshotErrors(errors));
}
