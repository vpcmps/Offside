using Microsoft.ApplicationInsights.DataContracts;

namespace Offside.ApplicationInsights;

/// <summary>The default severity of each failure species.</summary>
internal static class ErrorKindSeverity
{
    public static SeverityLevel Default(ErrorKind kind) => kind switch
    {
        ErrorKind.Unexpected => SeverityLevel.Critical,
        ErrorKind.ServiceUnavailable => SeverityLevel.Error,
        ErrorKind.Timeout => SeverityLevel.Error,
        ErrorKind.Unauthorized => SeverityLevel.Warning,
        ErrorKind.Forbidden => SeverityLevel.Warning,
        ErrorKind.TooManyRequests => SeverityLevel.Warning,
        ErrorKind.Conflict => SeverityLevel.Warning,
        ErrorKind.PreconditionFailed => SeverityLevel.Warning,
        ErrorKind.Gone => SeverityLevel.Warning,
        ErrorKind.Unprocessable => SeverityLevel.Warning,
        ErrorKind.NotFound => SeverityLevel.Information,
        ErrorKind.Validation => SeverityLevel.Information,
        ErrorKind.BadRequest => SeverityLevel.Information,
        _ => SeverityLevel.Error
    };
}
