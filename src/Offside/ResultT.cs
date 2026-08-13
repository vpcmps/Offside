using System.Linq;

namespace Offside;

public readonly struct Result<T>
{
    private readonly T _value;
    private readonly IReadOnlyList<Error> _errors;
    private readonly bool _isSuccess;

    private Result(bool isSuccess, T value, IReadOnlyList<Error> errors)
    {
        _isSuccess = isSuccess;
        _value = value;
        _errors = errors;
    }

    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;
    public T Value => _isSuccess
        ? _value
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");
    public IReadOnlyList<Error> Errors => _errors ?? Array.Empty<Error>();

    public bool TryGetValue(out T value)
    {
        if (_isSuccess)
        {
            value = _value;
            return true;
        }

        value = default!;
        return false;
    }

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        _isSuccess ? onSuccess(_value) : onFailure(Errors);

    public static Result<T> Success(T value) => new Result<T>(true, value, Array.Empty<Error>());

    public static Result<T> Failure(params Error[] errors) => Failure((IEnumerable<Error>)errors);

    public static Result<T> Failure(IEnumerable<Error> errors)
    {
        var list = errors as IReadOnlyList<Error> ?? errors.ToList();
        if (list.Count == 0)
            throw new ArgumentException("Failure requires at least one error.", nameof(errors));
        return new Result<T>(false, default!, list);
    }
}
