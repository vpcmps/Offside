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
        400, 401, 403, 404, 409, 410, 412, 422, 429, 500, 503, 504
    ];

    /// <summary>Maps a kind to its HTTP status code.</summary>
    /// <param name="kind">The failure species.</param>
    /// <returns>The status code.</returns>
    public static int StatusCode(ErrorKind kind) => ErrorSeverity.StatusCode(kind);

    /// <summary>
    /// Picks the primary error: the most severe <see cref="ErrorKind"/> present, with ties
    /// broken in favour of the first error in <paramref name="errors"/>.
    /// </summary>
    /// <param name="errors">The errors carried by a failed result. Must contain at least one.</param>
    /// <returns>The error that drives the HTTP status and Problem Details title.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Error SelectPrimary(IReadOnlyList<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
            throw new ArgumentException("A primary error requires at least one error.", nameof(errors));

        return ErrorSeverity.SelectPrimary(errors);
    }
}
