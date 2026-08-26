using System.Globalization;

namespace Offside.ApplicationInsights;

/// <summary>Controls how an <see cref="Error"/> is written to Application Insights.</summary>
public sealed class OffsideApplicationInsightsOptions
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
    /// telemetry is retained far longer than a request. Prefer <see cref="IncludeArgumentKeys"/>
    /// when only a few keys are safe.
    /// </remarks>
    public bool IncludeArguments { get; set; }

    /// <summary>
    /// Gets or sets the argument names written as <c>offside.arg.{name}</c> when
    /// <see cref="IncludeArguments"/> is <see langword="false"/>. Ignored when
    /// <see cref="IncludeArguments"/> is <see langword="true"/>. Defaults to empty.
    /// </summary>
    public IReadOnlyCollection<string> IncludeArgumentKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the culture used to resolve the message written as the trace text.
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
    /// Gets or sets the text of the trace, from the error and its resolved message. Defaults to
    /// <see cref="DomainErrorMessageFormat.MessageOnly"/>.
    /// </summary>
    /// <remarks>
    /// Pick <see cref="DomainErrorMessageFormat.CodePrefixed"/> when a human reads these lines raw,
    /// or supply your own. This shapes the trace text only: the code, kind, and field always travel
    /// as dimensions, whatever the format.
    /// </remarks>
    public Func<Error, string, string> FormatMessage { get; set; } = DomainErrorMessageFormat.MessageOnly;

    internal string Property(string name) => PropertyPrefix + name;
}
