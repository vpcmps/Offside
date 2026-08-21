namespace Offside.AspNetCore;

/// <summary>
/// HTTP mapping for <see cref="ErrorKind"/>: status codes and the distinct set used as
/// expected responses on the wire.
/// </summary>
public static class OffsideHttp
{
    /// <summary>
    /// The distinct HTTP status codes Offside can produce, in ascending order.
    /// </summary>
    public static IReadOnlyList<int> StatusCodes { get; } =
    [
        400, 401, 403, 404, 409, 410, 412, 422, 429, 500
    ];

    /// <summary>Maps a kind to its HTTP status code.</summary>
    /// <param name="kind">The failure species.</param>
    /// <returns>The status code.</returns>
    public static int StatusCode(ErrorKind kind) => ErrorSeverity.StatusCode(kind);
}
