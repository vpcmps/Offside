namespace Offside.Refit;

/// <summary>
/// Runs a call to a dependency and reports its failure as a <see cref="Result"/> instead of an
/// exception. Inject this rather than writing <c>try</c>/<c>catch</c> around each Refit client.
/// </summary>
/// <remarks>
/// Only failures that describe the dependency are converted: Refit's <c>ApiException</c>, a
/// cancelled or timed-out request, and a transport failure. Anything else — a bug in your own
/// callback, for instance — propagates untouched.
/// </remarks>
public interface IExternalApiCaller
{
    /// <summary>Runs a call that produces a value.</summary>
    /// <typeparam name="T">The value the dependency returns.</typeparam>
    /// <param name="call">The Refit call, given the cancellation token to honour.</param>
    /// <param name="options">Per-call mapping options. The registered defaults are used when omitted.</param>
    /// <param name="cancellationToken">Cancels the call. A cancellation the caller requested is rethrown, not mapped.</param>
    /// <returns>The value on success, or a failure carrying the mapped errors.</returns>
    Task<Result<T>> CallAsync<T>(
        Func<CancellationToken, Task<T>> call,
        OffsideRefitOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Runs a call that produces no value.</summary>
    /// <param name="call">The Refit call, given the cancellation token to honour.</param>
    /// <param name="options">Per-call mapping options. The registered defaults are used when omitted.</param>
    /// <param name="cancellationToken">Cancels the call. A cancellation the caller requested is rethrown, not mapped.</param>
    /// <returns>A success, or a failure carrying the mapped errors.</returns>
    Task<Result> CallAsync(
        Func<CancellationToken, Task> call,
        OffsideRefitOptions? options = null,
        CancellationToken cancellationToken = default);
}
