namespace Offside;

public enum ErrorKind
{
    Unexpected,
    Unauthorized,
    Forbidden,
    TooManyRequests,
    Conflict,
    PreconditionFailed,
    Gone,
    Unprocessable,
    NotFound,
    Validation,
    BadRequest
}
