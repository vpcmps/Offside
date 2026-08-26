namespace Offside.OpenTelemetry;

/// <summary>The default severity of each failure species.</summary>
/// <remarks>
/// Kept identical to the table in <c>Offside.ApplicationInsights</c>: a host that moves from the
/// classic SDK to OpenTelemetry must not see its severities shift underneath it.
/// </remarks>
internal static class ErrorKindSeverity
{
    public static DomainErrorSeverity Default(ErrorKind kind) => kind switch
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
}
