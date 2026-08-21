namespace Offside;

/// <summary>
/// The closed set of domain failure species. A kind selects the HTTP status code and the
/// severity rank used to pick the primary error when a <see cref="Result"/> carries several.
/// </summary>
/// <remarks>
/// Business rules reuse an existing kind through <see cref="Error.Custom"/>; they do not
/// invent new kinds. Declaration order is not severity order.
/// </remarks>
public enum ErrorKind
{
    /// <summary>An unhandled or infrastructure failure. Maps to HTTP 500 and is the most severe kind.</summary>
    Unexpected,

    /// <summary>The caller is not authenticated. Maps to HTTP 401.</summary>
    Unauthorized,

    /// <summary>The caller is authenticated but not allowed. Maps to HTTP 403.</summary>
    Forbidden,

    /// <summary>The caller exceeded a rate limit or quota. Maps to HTTP 429.</summary>
    TooManyRequests,

    /// <summary>The request conflicts with the current state of the resource. Maps to HTTP 409.</summary>
    Conflict,

    /// <summary>A precondition supplied by the caller was not met. Maps to HTTP 412.</summary>
    PreconditionFailed,

    /// <summary>The resource existed but has been permanently removed. Maps to HTTP 410.</summary>
    Gone,

    /// <summary>The request is well formed but semantically cannot be processed. Maps to HTTP 422.</summary>
    Unprocessable,

    /// <summary>The requested resource does not exist. Maps to HTTP 404.</summary>
    NotFound,

    /// <summary>A field failed validation. Maps to HTTP 400.</summary>
    Validation,

    /// <summary>The request itself is malformed. Maps to HTTP 400 and is the least severe kind.</summary>
    BadRequest
}
