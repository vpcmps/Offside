using System.Linq;

namespace Offside;

public readonly struct Result
{
    private readonly IReadOnlyList<Error> _errors;
    private readonly bool _failed;

    private Result(bool failed, IReadOnlyList<Error> errors)
    {
        _failed = failed;
        _errors = errors;
    }

    public bool IsSuccess => !_failed;
    public bool IsFailure => _failed;
    public IReadOnlyList<Error> Errors => _errors ?? Array.Empty<Error>();

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        !_failed ? onSuccess() : onFailure(Errors);

    public static Result Success() => new Result(false, Array.Empty<Error>());

    public static Result Failure(params Error[] errors) => Failure((IEnumerable<Error>)errors);

    public static Result Failure(IEnumerable<Error> errors) =>
        new Result(true, SnapshotErrors(errors));

    internal static IReadOnlyList<Error> SnapshotErrors(IEnumerable<Error> errors)
    {
        var copy = errors.ToArray();
        if (copy.Length == 0)
            throw new ArgumentException("Failure requires at least one error.", nameof(errors));
        return copy;
    }
}
