namespace Offside.OpenTelemetry;

/// <summary>
/// The names Offside emits telemetry under. The meter in particular must be registered with the
/// host's OpenTelemetry pipeline, otherwise the counter is produced and dropped.
/// </summary>
/// <remarks>
/// There is no Offside activity source to register: the integration never starts a span of its
/// own, it attaches an <see cref="ErrorEventName"/> event to whichever activity the host's
/// instrumentation already has in scope.
/// </remarks>
/// <example>
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithMetrics(metrics =&gt; metrics.AddMeter(OffsideTelemetry.MeterName))
///     .UseAzureMonitor();
/// </code>
/// </example>
public static class OffsideTelemetry
{
    /// <summary>The meter the error counter is published on.</summary>
    public const string MeterName = "Offside";

    /// <summary>The logger category domain errors are written under.</summary>
    public const string LoggerCategory = "Offside";

    /// <summary>The name of the counter incremented once per recorded error.</summary>
    public const string ErrorCounterName = "offside.errors";

    /// <summary>The name of the activity event added to the activity in scope.</summary>
    public const string ErrorEventName = "offside.error";
}
