namespace Offside;

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

    public bool IsSuccess => !_failed;
    public bool IsFailure => _failed;
    public T Value => !_failed
        ? _value
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");
    public IReadOnlyList<Error> Errors => _errors ?? Array.Empty<Error>();

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

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        !_failed ? onSuccess(_value) : onFailure(Errors);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        !_failed ? Result<TOut>.Success(map(Value)) : Result<TOut>.Failure(Errors);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind) =>
        !_failed ? bind(Value) : Result<TOut>.Failure(Errors);

    public static Result<T> Success(T value) => new Result<T>(false, value, Array.Empty<Error>());

    public static Result<T> Failure(params Error[] errors) => Failure((IEnumerable<Error>)errors);

    public static Result<T> Failure(IEnumerable<Error> errors) =>
        new Result<T>(true, default!, Result.SnapshotErrors(errors));
}
