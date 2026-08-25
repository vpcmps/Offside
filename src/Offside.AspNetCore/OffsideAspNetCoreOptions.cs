using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Offside.AspNetCore;

/// <summary>
/// Options controlling how Offside renders Problem Details responses.
/// </summary>
public sealed class OffsideAspNetCoreOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the diagnostic detail of an
    /// <see cref="ErrorKind.Unexpected"/> error is echoed back to the client in the
    /// <c>debug</c> field. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// Leave this off in production. The client-facing <c>detail</c> is always the generic
    /// catalog message for <c>unexpected</c>, regardless of this setting; only <c>debug</c>
    /// is gated by it.
    /// </remarks>
    public bool ExposeExceptionDetails { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Offside logs <see cref="ErrorKind.Unexpected"/>
    /// failures through <c>ILoggerFactory</c> under the category <c>Offside.AspNetCore</c>.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Set this to <see langword="false"/> when <see cref="OnProblem"/> owns telemetry, so the
    /// built-in line is not duplicated. Leaving both this <see langword="false"/> and
    /// <see cref="OnProblem"/> unset means a 500 is silent.
    /// </remarks>
    public bool LogUnexpected { get; set; } = true;

    /// <summary>
    /// Gets or sets a callback that runs after the problem document is built and may add
    /// fields through <see cref="OffsideProblem.Extensions"/> (and
    /// <see cref="OffsideProblem.Item.Extensions"/>). Core properties stay init-only.
    /// </summary>
    /// <remarks>
    /// Use JSON-safe primitives. Keys that collide with the Problem Details contract
    /// (<c>type</c>, <c>title</c>, <c>status</c>, <c>detail</c>, <c>instance</c>,
    /// <c>traceId</c>, <c>errorCode</c>, <c>debug</c>, <c>errors</c>) are stripped before
    /// the response is written. Exceptions are logged and do not replace the problem document.
    /// Hooks are applied only when the request's <c>HttpContext</c> is available — the
    /// <c>bool exposeExceptionDetails</c> overloads construct options without this callback.
    /// </remarks>
    public Action<OffsideProblem, IReadOnlyList<Error>>? CustomizeProblem { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked after <see cref="CustomizeProblem"/> and before the
    /// response is written. Use it for host telemetry. Do not write to the response body.
    /// </summary>
    /// <remarks>
    /// Exceptions are logged and do not replace the problem document. The callback is applied
    /// only when the request's <c>HttpContext</c> is available.
    /// </remarks>
    public Action<OffsideProblem, IReadOnlyList<Error>, HttpContext>? OnProblem { get; set; }

    /// <summary>
    /// Gets or sets a callback that supplies the <c>traceId</c> written on the problem document.
    /// When <see langword="null"/>, Offside uses <c>Activity.Current.TraceId</c> (32 hex) and
    /// falls back to <c>HttpContext.TraceIdentifier</c>.
    /// </summary>
    /// <remarks>
    /// Restore the W3C traceparent with <c>context => Activity.Current?.Id ?? context.TraceIdentifier</c>.
    /// </remarks>
    public Func<HttpContext, string>? ResolveTraceId { get; set; }

    /// <summary>
    /// Creates options whose <see cref="ExposeExceptionDetails"/> follows
    /// <c>environment.IsDevelopment()</c>.
    /// </summary>
    /// <param name="environment">The host environment.</param>
    /// <returns>The options.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="environment"/> is <see langword="null"/>.</exception>
    public static OffsideAspNetCoreOptions FromEnvironment(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return new OffsideAspNetCoreOptions
        {
            ExposeExceptionDetails = environment.IsDevelopment()
        };
    }
}
