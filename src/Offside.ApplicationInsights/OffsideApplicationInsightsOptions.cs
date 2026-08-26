using System.Globalization;
using Microsoft.ApplicationInsights.DataContracts;

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
    /// telemetry is retained far longer than a request.
    /// </remarks>
    public bool IncludeArguments { get; set; }

    /// <summary>
    /// Gets or sets the culture used to resolve the message written as the trace text.
    /// Defaults to <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    /// <remarks>Logs read best in one stable language; this is deliberately not the request culture.</remarks>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets or sets the severity chosen for a kind. Defaults to
    /// <see cref="ErrorKind.Unexpected"/> → <c>Critical</c>, <c>ServiceUnavailable</c> and
    /// <c>Timeout</c> → <c>Error</c>, <c>NotFound</c>, <c>Validation</c>, and <c>BadRequest</c>
    /// → <c>Information</c>, everything else → <c>Warning</c>.
    /// </summary>
    public Func<ErrorKind, SeverityLevel> SeverityFor { get; set; } = ErrorKindSeverity.Default;

    internal string Property(string name) => PropertyPrefix + name;
}
