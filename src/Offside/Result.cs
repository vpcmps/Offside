using System.Linq;

namespace Offside;

public readonly struct Result
{
    private readonly IReadOnlyList<Error> _errors;
    private readonly bool _isSuccess;

    private Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        _isSuccess = isSuccess;
        _errors = errors;
    }

    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;
    public IReadOnlyList<Error> Errors => _errors ?? Array.Empty<Error>();

    public static Result Success() => new Result(true, Array.Empty<Error>());

    public static Result Failure(params Error[] errors) => Failure((IEnumerable<Error>)errors);

    public static Result Failure(IEnumerable<Error> errors)
    {
        var list = errors as IReadOnlyList<Error> ?? errors.ToList();
        if (list.Count == 0)
            throw new ArgumentException("Failure requires at least one error.", nameof(errors));
        return new Result(false, list);
    }
}
