using System.Net;

namespace Offside.Refit;

/// <summary>
/// The HTTP-to-domain mapping used when a dependency answers with a failure: the inverse of the
/// <c>ErrorKind</c> to status mapping that Offside applies on the way out.
/// </summary>
/// <remarks>
/// The mapping mirrors what the dependency said — a 404 from the dependency becomes
/// <see cref="ErrorKind.NotFound"/>. Deciding whether that should surface to your own caller,
/// or be folded into <see cref="ErrorKind.ServiceUnavailable"/>, is the calling code's job.
/// </remarks>
public static class OffsideRefit
{
    /// <summary>Maps an HTTP status code returned by a dependency to a failure species.</summary>
    /// <param name="statusCode">The status code the dependency answered with.</param>
    /// <returns>
    /// The matching kind. Unmapped 4xx codes become <see cref="ErrorKind.BadRequest"/> and
    /// unmapped 5xx codes become <see cref="ErrorKind.Unexpected"/>.
    /// </returns>
    public static ErrorKind Kind(HttpStatusCode statusCode) => (int)statusCode switch
    {
        400 => ErrorKind.BadRequest,
        401 => ErrorKind.Unauthorized,
        403 => ErrorKind.Forbidden,
        404 => ErrorKind.NotFound,
        409 => ErrorKind.Conflict,
        410 => ErrorKind.Gone,
        412 => ErrorKind.PreconditionFailed,
        422 => ErrorKind.Unprocessable,
        429 => ErrorKind.TooManyRequests,
        502 => ErrorKind.ServiceUnavailable,
        503 => ErrorKind.ServiceUnavailable,
        504 => ErrorKind.Timeout,
        >= 500 => ErrorKind.Unexpected,
        >= 400 => ErrorKind.BadRequest,
        _ => ErrorKind.Unexpected
    };

    /// <summary>The catalog code suffix for a kind, such as <c>not_found</c> for a 404.</summary>
    /// <param name="kind">The failure species.</param>
    /// <returns>The suffix, which <see cref="OffsideRefitOptions.CodePrefix"/> is prepended to.</returns>
    public static string CodeSuffix(ErrorKind kind) => kind switch
    {
        ErrorKind.Unexpected => "unexpected",
        ErrorKind.Unauthorized => "unauthorized",
        ErrorKind.Forbidden => "forbidden",
        ErrorKind.TooManyRequests => "too_many_requests",
        ErrorKind.Conflict => "conflict",
        ErrorKind.PreconditionFailed => "precondition_failed",
        ErrorKind.Gone => "gone",
        ErrorKind.Unprocessable => "unprocessable",
        ErrorKind.NotFound => "not_found",
        ErrorKind.Validation => "validation",
        ErrorKind.BadRequest => "bad_request",
        ErrorKind.ServiceUnavailable => "service_unavailable",
        ErrorKind.Timeout => "timeout",
        _ => "unexpected"
    };
}
