using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Offside.AspNetCore;

/// <summary>
/// Options controlling how Offside renders Problem Details responses.
/// </summary>
public sealed class OffsideAspNetCoreOptions
{
    private bool _logUnexpected = true;
    private bool _logUnexpectedSpecified;

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
    /// Defaults to <see langword="true"/> when no <see cref="IDomainErrorRecorder"/> is
    /// registered, and to <see langword="false"/> when one is — so the built-in line is not
    /// duplicated. Set this explicitly to keep both, or to silence both.
    /// </summary>
    public bool LogUnexpected
    {
        get => _logUnexpected;
        set
        {
            _logUnexpected = value;
            _logUnexpectedSpecified = true;
        }
    }

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
    /// response is written. Use it for host-specific work. Do not write to the response body,
    /// and do not emit the domain-error event here — that is <see cref="IDomainErrorRecorder"/>.
    /// </summary>
    /// <remarks>
    /// Exceptions are logged and do not replace the problem document. The callback is applied
    /// only when the request's <c>HttpContext</c> is available.
    /// </remarks>
    public Action<OffsideProblem, IReadOnlyList<Error>, HttpContext>? OnProblem { get; set; }

    /// <summary>
    /// Gets or sets extra telemetry dimensions merged into every pipeline recording, such as an
    /// operation name. Offside dimensions always win over these keys.
    /// </summary>
    public Func<OffsideProblem, IReadOnlyList<Error>, HttpContext, IReadOnlyDictionary<string, string>>? TelemetryProperties { get; set; }

    /// <summary>
    /// Gets or sets brownfield aliases flattened into the problem document
    /// (<c>message</c>, <c>errors[].name</c>, <c>errors[].reason</c>, <c>technicalDetail</c>).
    /// Defaults to <see cref="LegacyProblemAliases.None"/>.
    /// </summary>
    public LegacyProblemAliases LegacyAliases { get; set; } = LegacyProblemAliases.None;

    /// <summary>
    /// Gets or sets the <c>errors[].name</c> written for a field-less error when
    /// <see cref="LegacyAliases"/> is <see cref="LegacyProblemAliases.MessageReasonAndTechnicalDetail"/>.
    /// Defaults to <c>generalErrors</c> — the FastEndpoints sentinel. Set to
    /// <see langword="null"/> or empty to omit <c>name</c> when <see cref="Error.Field"/> is null.
    /// </summary>
    /// <remarks>
    /// The canonical <c>field</c> stays null. Only the legacy alias carries the sentinel.
    /// </remarks>
    public string? LegacyGeneralErrorName { get; set; } = "generalErrors";

    /// <summary>
    /// Gets or sets how many times the HTTP problem pipeline calls
    /// <see cref="IDomainErrorRecorder"/> for one failed result.
    /// Defaults to <see cref="ProblemRecordMode.PerError"/>.
    /// </summary>
    /// <remarks>
    /// This applies only to <c>ToHttpResult</c>, <c>ToActionResult</c>, and
    /// <c>SendOffsideAsync</c>. <c>Result.RecordTo</c> and MediatR publication stay one
    /// entry per error.
    /// </remarks>
    public ProblemRecordMode RecordMode { get; set; }

    /// <summary>
    /// Gets or sets a callback that supplies the <c>traceId</c> written on the problem document.
    /// When <see langword="null"/>, Offside uses <c>Activity.Current.TraceId</c> (32 hex) and
    /// falls back to <c>HttpContext.TraceIdentifier</c>.
    /// </summary>
    /// <remarks>
    /// Restore the W3C traceparent with <c>context => Activity.Current?.Id ?? context.TraceIdentifier</c>.
    /// </remarks>
    public Func<HttpContext, string>? ResolveTraceId { get; set; }

    internal bool ShouldLogUnexpected(bool recorderRegistered) =>
        _logUnexpectedSpecified ? _logUnexpected : !recorderRegistered;

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

/// <summary>Brownfield Problem Details aliases for hosts that cannot change the client yet.</summary>
[Flags]
public enum LegacyProblemAliases
{
    /// <summary>No extra fields.</summary>
    None = 0,

    /// <summary>
    /// Adds <c>message</c> (= <c>detail</c>), per-item <c>reason</c> (= <c>detail</c>),
    /// per-item <c>name</c> (= <c>field</c>, or <see cref="OffsideAspNetCoreOptions.LegacyGeneralErrorName"/>
    /// when the error has no field), and <c>technicalDetail</c> only when <c>debug</c> is present.
    /// </summary>
    MessageReasonAndTechnicalDetail = 1
}

/// <summary>
/// How the HTTP problem pipeline records a failed result through
/// <see cref="IDomainErrorRecorder"/>.
/// </summary>
public enum ProblemRecordMode
{
    /// <summary>
    /// Records every error in the result, in result order. The default.
    /// A validation failure on N fields produces N traces, events, and counter increments.
    /// </summary>
    PerError = 0,

    /// <summary>
    /// Records only the error that drives the HTTP status, chosen by
    /// <see cref="OffsideHttp.SelectPrimary"/>. Use this when alerting on request failure
    /// rather than on each field.
    /// </summary>
    PrimaryErrorOnly = 1
}
