namespace Offside.AspNetCore;

internal static class ErrorSeverity
{
    public static Error SelectPrimary(IReadOnlyList<Error> errors) =>
        errors.MinBy(error => Rank(error.Kind))!;

    public static int StatusCode(ErrorKind kind) => kind switch
    {
        ErrorKind.Unexpected => 500,
        ErrorKind.Unauthorized => 401,
        ErrorKind.Forbidden => 403,
        ErrorKind.TooManyRequests => 429,
        ErrorKind.Conflict => 409,
        ErrorKind.PreconditionFailed => 412,
        ErrorKind.Gone => 410,
        ErrorKind.Unprocessable => 422,
        ErrorKind.NotFound => 404,
        ErrorKind.Validation => 400,
        ErrorKind.BadRequest => 400,
        ErrorKind.ServiceUnavailable => 503,
        ErrorKind.Timeout => 504,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    internal static int Rank(ErrorKind kind) => kind switch
    {
        ErrorKind.Unexpected => 0,
        ErrorKind.Unauthorized or ErrorKind.Forbidden => 1,
        ErrorKind.TooManyRequests => 2,
        ErrorKind.ServiceUnavailable or ErrorKind.Timeout => 3,
        ErrorKind.Conflict => 4,
        ErrorKind.PreconditionFailed => 5,
        ErrorKind.Gone => 6,
        ErrorKind.Unprocessable => 7,
        ErrorKind.NotFound => 8,
        ErrorKind.Validation or ErrorKind.BadRequest => 9,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
