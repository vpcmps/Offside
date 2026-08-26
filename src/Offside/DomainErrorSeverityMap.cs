namespace Offside;

/// <summary>
/// Ready-made <see cref="ErrorKind"/> → <see cref="DomainErrorSeverity"/> maps for
/// <c>SeverityFor</c> on the telemetry integrations.
/// </summary>
public static class DomainErrorSeverityMap
{
    /// <summary>
    /// The library default: a validation or lookup failure is the system working
    /// (<see cref="DomainErrorSeverity.Information"/>), a dependency outage is
    /// <see cref="DomainErrorSeverity.Error"/>, and <see cref="ErrorKind.Unexpected"/> is
    /// <see cref="DomainErrorSeverity.Critical"/>.
    /// </summary>
    public static DomainErrorSeverity Library(ErrorKind kind) => kind switch
    {
        ErrorKind.Unexpected => DomainErrorSeverity.Critical,
        ErrorKind.ServiceUnavailable => DomainErrorSeverity.Error,
        ErrorKind.Timeout => DomainErrorSeverity.Error,
        ErrorKind.Unauthorized => DomainErrorSeverity.Warning,
        ErrorKind.Forbidden => DomainErrorSeverity.Warning,
        ErrorKind.TooManyRequests => DomainErrorSeverity.Warning,
        ErrorKind.Conflict => DomainErrorSeverity.Warning,
        ErrorKind.PreconditionFailed => DomainErrorSeverity.Warning,
        ErrorKind.Gone => DomainErrorSeverity.Warning,
        ErrorKind.Unprocessable => DomainErrorSeverity.Warning,
        ErrorKind.NotFound => DomainErrorSeverity.Information,
        ErrorKind.Validation => DomainErrorSeverity.Information,
        ErrorKind.BadRequest => DomainErrorSeverity.Information,
        _ => DomainErrorSeverity.Error
    };

    /// <summary>
    /// Recusals — including 404 and 400 — are <see cref="DomainErrorSeverity.Warning"/> so they
    /// still group in operations views. Unexpected failures are <see cref="DomainErrorSeverity.Error"/>
    /// rather than Critical. Outages stay Error. Use this when paging on Information would miss
    /// business refusals, or paging on Critical would over-alert 500s.
    /// </summary>
    public static DomainErrorSeverity Operations(ErrorKind kind) => kind switch
    {
        ErrorKind.Unexpected => DomainErrorSeverity.Error,
        ErrorKind.ServiceUnavailable => DomainErrorSeverity.Error,
        ErrorKind.Timeout => DomainErrorSeverity.Error,
        ErrorKind.Unauthorized => DomainErrorSeverity.Warning,
        ErrorKind.Forbidden => DomainErrorSeverity.Warning,
        ErrorKind.TooManyRequests => DomainErrorSeverity.Warning,
        ErrorKind.Conflict => DomainErrorSeverity.Warning,
        ErrorKind.PreconditionFailed => DomainErrorSeverity.Warning,
        ErrorKind.Gone => DomainErrorSeverity.Warning,
        ErrorKind.Unprocessable => DomainErrorSeverity.Warning,
        ErrorKind.NotFound => DomainErrorSeverity.Warning,
        ErrorKind.Validation => DomainErrorSeverity.Warning,
        ErrorKind.BadRequest => DomainErrorSeverity.Warning,
        _ => DomainErrorSeverity.Error
    };
}
