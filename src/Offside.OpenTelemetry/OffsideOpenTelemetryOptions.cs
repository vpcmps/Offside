using System.Globalization;

namespace Offside.OpenTelemetry;

/// <summary>Controls how an <see cref="Error"/> is emitted through OpenTelemetry.</summary>
public sealed class OffsideOpenTelemetryOptions
{
    /// <summary>
    /// Gets or sets the prefix of every Offside dimension. Defaults to <c>offside.</c>, producing
    /// <c>offside.code</c>, <c>offside.errorCode</c>, <c>offside.kind</c>, and <c>offside.field</c>.
    /// </summary>
    public string PropertyPrefix { get; set; } = "offside.";

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Error.Arguments"/> are written as
    /// <c>offside.arg.{name}</c> dimensions. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// Leave this off unless you know every argument is safe to store. Arguments carry whatever
    /// the domain put in them — identifiers, attempted values, reasons from a dependency — and
    /// telemetry is retained far longer than a request. Arguments never reach the counter,
    /// whatever this is set to. Prefer <see cref="IncludeArgumentKeys"/> when only a few keys
    /// are safe.
    /// </remarks>
    public bool IncludeArguments { get; set; }

    /// <summary>
    /// Gets or sets the argument names written as <c>offside.arg.{name}</c> when
    /// <see cref="IncludeArguments"/> is <see langword="false"/>. Ignored when
    /// <see cref="IncludeArguments"/> is <see langword="true"/>. Defaults to empty.
    /// </summary>
    public IReadOnlyCollection<string> IncludeArgumentKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the culture used to resolve the message written as the log text.
    /// Defaults to <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    /// <remarks>Logs read best in one stable language; this is deliberately not the request culture.</remarks>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets or sets the severity chosen for a kind. Defaults to
    /// <see cref="DomainErrorSeverityMap.Library"/>.
    /// </summary>
    public Func<ErrorKind, DomainErrorSeverity> SeverityFor { get; set; } = DomainErrorSeverityMap.Library;

    /// <summary>
    /// Gets or sets the text of the log entry, from the error and its resolved message. Defaults to
    /// <see cref="DomainErrorMessageFormat.MessageOnly"/>.
    /// </summary>
    /// <remarks>
    /// Pick <see cref="DomainErrorMessageFormat.CodePrefixed"/> when a human reads these lines raw,
    /// or supply your own. This shapes the log line only: the code, kind, and field always travel
    /// as dimensions, whatever the format.
    /// </remarks>
    public Func<Error, string, string> FormatMessage { get; set; } = DomainErrorMessageFormat.MessageOnly;

    /// <summary>
    /// Gets or sets a value indicating whether the error is written to
    /// <see cref="Microsoft.Extensions.Logging.ILogger"/> under
    /// <see cref="OffsideTelemetry.LoggerCategory"/>. Defaults to <see langword="true"/>.
    /// </summary>
    public bool EmitLog { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether an <see cref="OffsideTelemetry.ErrorEventName"/>
    /// event is added to the current activity. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>With no activity in scope the event is skipped; that is not an error.</remarks>
    public bool EmitActivityEvent { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="OffsideTelemetry.ErrorCounterName"/> is
    /// incremented. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// The counter carries only <c>offside.kind</c> and <c>offside.code</c>. Field, arguments, and
    /// caller-supplied dimensions are left off on purpose: a metric with open-ended cardinality is
    /// expensive to ingest and slow to query.
    /// </remarks>
    public bool EmitMetric { get; set; } = true;

    /// <summary>
    /// Gets or sets when recording an error also marks the current activity as failed.
    /// Defaults to <see cref="ActivityFailurePolicy.None"/>.
    /// </summary>
    public ActivityFailurePolicy ActivityFailure { get; set; } = ActivityFailurePolicy.None;

    /// <summary>
    /// Gets or sets a value indicating whether recording an error at or above
    /// <see cref="MinimumSeverityForActivityFailure"/> also marks the current activity as failed.
    /// Defaults to <see langword="false"/>. Prefer <see cref="ActivityFailure"/>.
    /// </summary>
    /// <remarks>
    /// A domain failure is often a perfectly successful request — a 404 answered correctly is not a
    /// broken operation. Turn this on only where a recorded error really does mean the span failed.
    /// </remarks>
    public bool SetActivityStatusOnError { get; set; }

    /// <summary>
    /// Gets or sets the severity from which an error marks the activity as failed, when
    /// <see cref="SetActivityStatusOnError"/> is on or
    /// <see cref="ActivityFailure"/> is <see cref="ActivityFailurePolicy.FromSeverity"/>.
    /// Defaults to <see cref="DomainErrorSeverity.Error"/>.
    /// </summary>
    public DomainErrorSeverity MinimumSeverityForActivityFailure { get; set; } = DomainErrorSeverity.Error;

    internal string Property(string name) => PropertyPrefix + name;
}

/// <summary>When a recorded error marks the current <c>Activity</c> as failed.</summary>
public enum ActivityFailurePolicy
{
    /// <summary>Never. The default — a correctly answered 404 is not a broken operation.</summary>
    None = 0,

    /// <summary>
    /// Mark the span for <see cref="ErrorKind.Unexpected"/>,
    /// <see cref="ErrorKind.ServiceUnavailable"/>, and <see cref="ErrorKind.Timeout"/> — the
    /// kinds that map to HTTP 5xx. Use this when migrating from exceptions so 503s stay in the
    /// error rate.
    /// </summary>
    ServerErrors = 1,

    /// <summary>Mark the span when severity is at least <c>MinimumSeverityForActivityFailure</c>.</summary>
    FromSeverity = 2
}
