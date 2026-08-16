using System.Linq;

namespace Offside;

/// <summary>
/// The outcome of an operation that produces no value: either success, or failure carrying
/// one or more <see cref="Error"/> instances.
/// </summary>
/// <remarks>
/// This is a value type, so <c>default(Result)</c> is a successful result. Use
/// <see cref="Result{T}"/> when the operation produces a value.
/// </remarks>
public readonly struct Result
{
    private readonly IReadOnlyList<Error> _errors;
    private readonly bool _failed;

    private Result(bool failed, IReadOnlyList<Error> errors)
    {
        _failed = failed;
        _errors = errors;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess => !_failed;

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => _failed;

    /// <summary>Gets the errors that caused the failure, or an empty list on success.</summary>
    public IReadOnlyList<Error> Errors => _errors ?? Array.Empty<Error>();

    /// <summary>Runs one of two functions depending on the outcome.</summary>
    /// <typeparam name="TOut">The type both branches return.</typeparam>
    /// <param name="onSuccess">Invoked when the result is a success.</param>
    /// <param name="onFailure">Invoked with the errors when the result is a failure.</param>
    /// <returns>The value produced by the branch that ran.</returns>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        !_failed ? onSuccess() : onFailure(Errors);

    /// <summary>Creates a successful result.</summary>
    /// <returns>The result.</returns>
    public static Result Success() => new Result(false, Array.Empty<Error>());

    /// <summary>Creates a failed result.</summary>
    /// <param name="errors">The errors, in the order they should be reported. At least one is required.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Result Failure(params Error[] errors) => Failure((IEnumerable<Error>)errors);

    /// <summary>Creates a failed result from a sequence, which is copied immediately.</summary>
    /// <param name="errors">The errors, in the order they should be reported. At least one is required.</param>
    /// <returns>The result.</returns>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Result Failure(IEnumerable<Error> errors) =>
        new Result(true, SnapshotErrors(errors));

    /// <summary>
    /// Merges several results, concatenating the errors of every failure in argument order.
    /// </summary>
    /// <param name="results">The results to merge.</param>
    /// <returns>A success when every input succeeded; otherwise a failure carrying all errors.</returns>
    public static Result Combine(params Result[] results)
    {
        var errors = new List<Error>();
        foreach (var result in results)
        {
            if (result.IsFailure)
                errors.AddRange(result.Errors);
        }
        return errors.Count == 0 ? Success() : Failure(errors);
    }

    /// <summary>
    /// Merges several value results, concatenating the errors of every failure in argument order.
    /// The values themselves are discarded.
    /// </summary>
    /// <typeparam name="T">The value type of the results being merged.</typeparam>
    /// <param name="results">The results to merge.</param>
    /// <returns>A success when every input succeeded; otherwise a failure carrying all errors.</returns>
    public static Result Combine<T>(params Result<T>[] results)
    {
        var errors = new List<Error>();
        foreach (var result in results)
        {
            if (result.IsFailure)
                errors.AddRange(result.Errors);
        }
        return errors.Count == 0 ? Success() : Failure(errors);
    }

    internal static IReadOnlyList<Error> SnapshotErrors(IEnumerable<Error> errors)
    {
        var copy = errors.ToArray();
        if (copy.Length == 0)
            throw new ArgumentException("Failure requires at least one error.", nameof(errors));
        return copy;
    }
}
