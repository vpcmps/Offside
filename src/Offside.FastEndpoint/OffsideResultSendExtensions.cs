using Microsoft.AspNetCore.Http;
using Offside.AspNetCore;

namespace Offside.FastEndpoint;

/// <summary>
/// Sends an Offside <see cref="Result"/> through the ASP.NET response pipeline.
/// </summary>
public static class OffsideResultSendExtensions
{
    /// <summary>
    /// Writes 204 on success, or an Offside problem document on failure.
    /// </summary>
    /// <param name="result">The domain result.</param>
    /// <param name="httpContext">The current request.</param>
    /// <param name="cancellationToken">Not used; present to match handler signatures.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="httpContext"/> is <see langword="null"/>.</exception>
    public static Task SendOffsideAsync(
        this Result result,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        _ = cancellationToken;
        return result.ToHttpResult(httpContext).ExecuteAsync(httpContext);
    }

    /// <summary>
    /// Writes 200 with the value on success, or an Offside problem document on failure.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The domain result.</param>
    /// <param name="httpContext">The current request.</param>
    /// <param name="cancellationToken">Not used; present to match handler signatures.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="httpContext"/> is <see langword="null"/>.</exception>
    public static Task SendOffsideAsync<T>(
        this Result<T> result,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        _ = cancellationToken;
        return result.ToHttpResult(httpContext).ExecuteAsync(httpContext);
    }
}
