namespace Offside.OpenTelemetry;

/// <summary>
/// How loud a recorded failure is. Deliberately mirrors the severity names of the classic
/// Application Insights SDK, so a host reading both integrations sees the same vocabulary.
/// </summary>
public enum DomainErrorSeverity
{
    /// <summary>Diagnostic detail, below <see cref="Information"/>.</summary>
    Verbose = 0,

    /// <summary>An expected outcome worth counting, such as a lookup that found nothing.</summary>
    Information = 1,

    /// <summary>Something the caller did wrong, or a state the domain refused.</summary>
    Warning = 2,

    /// <summary>A dependency or the process itself failed to do its job.</summary>
    Error = 3,

    /// <summary>An unexpected failure with no domain meaning.</summary>
    Critical = 4
}
