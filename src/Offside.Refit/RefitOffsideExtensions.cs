using System.Net;
using System.Net.Http;
using global::Refit;

namespace Offside.Refit;

/// <summary>
/// Maps Refit and transport failures onto Offside <see cref="Error"/> values, so calling code
/// reports a dependency failure the same way it reports its own domain failures.
/// </summary>
public static class RefitOffsideExtensions
{
    /// <summary>
    /// Maps a failed Refit call to one or more errors. When the dependency answered with an
    /// <c>application/problem+json</c> body and <see cref="OffsideRefitOptions.ReadProblemDetails"/>
    /// is on, its errors are restored one by one; otherwise the status code alone decides.
    /// </summary>
    /// <param name="exception">The exception Refit threw.</param>
    /// <param name="options">The mapping options. Defaults are used when omitted.</param>
    /// <returns>The errors, in the order the dependency reported them. Never empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<Error> ToOffsideErrors(
        this ApiException exception,
        OffsideRefitOptions? options = null)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));

        var resolved = options ?? OffsideRefitOptions.Default;
        var requestUri = exception.RequestMessage?.RequestUri;

        if (resolved.ReadProblemDetails)
        {
            var fromBody = ProblemDetailsReader.Read(exception.Content, exception.StatusCode, requestUri, resolved);
            if (fromBody is { Count: > 0 })
                return fromBody;
        }

        return new[] { FromStatus(exception.StatusCode, requestUri, exception.ReasonPhrase, resolved) };
    }

    /// <summary>Maps a failed Refit call to its primary error — the first one reported.</summary>
    /// <param name="exception">The exception Refit threw.</param>
    /// <param name="options">The mapping options. Defaults are used when omitted.</param>
    /// <returns>The first mapped error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static Error ToError(this ApiException exception, OffsideRefitOptions? options = null) =>
        exception.ToOffsideErrors(options)[0];

    /// <summary>Maps a failed Refit call to a failed <see cref="Result"/>.</summary>
    /// <param name="exception">The exception Refit threw.</param>
    /// <param name="options">The mapping options. Defaults are used when omitted.</param>
    /// <returns>A failure carrying every mapped error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static Result ToResult(this ApiException exception, OffsideRefitOptions? options = null) =>
        Result.Failure(exception.ToOffsideErrors(options));

    /// <summary>Maps a failed Refit call to a failed <see cref="Result{T}"/>.</summary>
    /// <typeparam name="T">The value the call would have produced.</typeparam>
    /// <param name="exception">The exception Refit threw.</param>
    /// <param name="options">The mapping options. Defaults are used when omitted.</param>
    /// <returns>A failure carrying every mapped error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static Result<T> ToResult<T>(this ApiException exception, OffsideRefitOptions? options = null) =>
        Result<T>.Failure(exception.ToOffsideErrors(options));

    /// <summary>
    /// Maps a transport failure — the dependency was never reached — to
    /// <see cref="ErrorKind.ServiceUnavailable"/>.
    /// </summary>
    /// <param name="exception">The transport exception.</param>
    /// <param name="options">The mapping options. Defaults are used when omitted.</param>
    /// <returns>The error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static Error ToOffsideError(this HttpRequestException exception, OffsideRefitOptions? options = null)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));

        var resolved = options ?? OffsideRefitOptions.Default;
        return Error.Custom(
            resolved.Code(OffsideRefit.CodeSuffix(ErrorKind.ServiceUnavailable)),
            ErrorKind.ServiceUnavailable,
            new { api = resolved.ApiName, reason = exception.Message });
    }

    internal static Error Timeout(OffsideRefitOptions options, Uri? requestUri, string? reason) =>
        Error.Custom(
            options.Code(OffsideRefit.CodeSuffix(ErrorKind.Timeout)),
            ErrorKind.Timeout,
            new { api = options.ApiName, requestUri = requestUri?.ToString(), reason });

    internal static Error FromStatus(
        HttpStatusCode statusCode,
        Uri? requestUri,
        string? reason,
        OffsideRefitOptions options)
    {
        var kind = OffsideRefit.Kind(statusCode);
        return Error.Custom(
            options.Code(OffsideRefit.CodeSuffix(kind)),
            kind,
            new
            {
                api = options.ApiName,
                status = (int)statusCode,
                requestUri = requestUri?.ToString(),
                reason
            });
    }
}
