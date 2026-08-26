using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Offside.OpenTelemetry;

internal sealed class OpenTelemetryDomainErrorRecorder : IDomainErrorRecorder
{
    private static readonly Meter Meter = new(OffsideTelemetry.MeterName);

    private static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>(
        OffsideTelemetry.ErrorCounterName,
        unit: "{error}",
        description: "Domain errors recorded through Offside.");

    private int _meterWarned;

    private readonly ILogger _logger;
    private readonly OffsideOpenTelemetryOptions _options;
    private readonly IErrorMessageResolver? _resolver;

    public OpenTelemetryDomainErrorRecorder(
        ILoggerFactory loggerFactory,
        OffsideOpenTelemetryOptions options,
        IErrorMessageResolver? resolver = null)
    {
        if (loggerFactory is null)
            throw new ArgumentNullException(nameof(loggerFactory));

        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = loggerFactory.CreateLogger(OffsideTelemetry.LoggerCategory);
        _resolver = resolver;
    }

    public void Record(Error error, IReadOnlyDictionary<string, string>? properties = null)
    {
        if (error is null)
            throw new ArgumentNullException(nameof(error));

        var severity = _options.SeverityFor(error.Kind);
        var dimensions = Dimensions(error, properties);

        if (_options.EmitLog)
            EmitLog(error, severity, dimensions);

        if (_options.EmitActivityEvent)
            EmitActivityEvent(error, severity, dimensions);

        if (_options.EmitMetric)
        {
            WarnIfMetricHasNoListener();
            EmitMetric(error);
        }
    }

    /// <summary>
    /// Builds the dimensions shared by the log entry and the activity event. Offside dimensions are
    /// written first and a caller key that collides with one is dropped, so a stray
    /// <c>offside.kind</c> from the call site can never shadow the real one.
    /// </summary>
    private KeyValuePair<string, object?>[] Dimensions(
        Error error,
        IReadOnlyDictionary<string, string>? properties)
    {
        var dimensions = new List<KeyValuePair<string, object?>>
        {
            new(_options.Property("code"), error.Code),
            new(_options.Property("errorCode"), error.ErrorCode),
            new(_options.Property("kind"), error.Kind.ToString())
        };

        if (error.Field is not null)
            dimensions.Add(new KeyValuePair<string, object?>(_options.Property("field"), error.Field));

        foreach (var argument in ErrorArgumentFilter.Select(error, _options.IncludeArguments, _options.IncludeArgumentKeys))
        {
            dimensions.Add(new KeyValuePair<string, object?>(
                _options.Property("arg." + argument.Key),
                Convert.ToString(argument.Value, CultureInfo.InvariantCulture) ?? string.Empty));
        }

        if (properties is not null)
        {
            var taken = new HashSet<string>(dimensions.Select(dimension => dimension.Key), StringComparer.Ordinal);

            foreach (var pair in properties)
            {
                if (taken.Add(pair.Key))
                    dimensions.Add(new KeyValuePair<string, object?>(pair.Key, pair.Value));
            }
        }

        return dimensions.ToArray();
    }

    private void EmitLog(Error error, DomainErrorSeverity severity, KeyValuePair<string, object?>[] dimensions)
    {
        var level = LogLevelFor(severity);

        if (!_logger.IsEnabled(level))
            return;

        _logger.Log(
            level,
            new EventId(0, error.Code),
            new DomainErrorLogState(_options.FormatMessage(error, Message(error)), dimensions),
            exception: null,
            formatter: static (state, _) => state.ToString());
    }

    private void EmitActivityEvent(
        Error error,
        DomainErrorSeverity severity,
        KeyValuePair<string, object?>[] dimensions)
    {
        var activity = Activity.Current;

        if (activity is null)
            return;

        activity.AddEvent(new ActivityEvent(
            OffsideTelemetry.ErrorEventName,
            tags: new ActivityTagsCollection(dimensions)));

        if (ShouldFailActivity(error.Kind, severity))
            activity.SetStatus(ActivityStatusCode.Error);
    }

    private bool ShouldFailActivity(ErrorKind kind, DomainErrorSeverity severity)
    {
        if (_options.ActivityFailure == ActivityFailurePolicy.ServerErrors
            && kind is ErrorKind.Unexpected or ErrorKind.ServiceUnavailable or ErrorKind.Timeout)
            return true;

        if (_options.ActivityFailure == ActivityFailurePolicy.FromSeverity
            && severity >= _options.MinimumSeverityForActivityFailure)
            return true;

        return _options.SetActivityStatusOnError
            && severity >= _options.MinimumSeverityForActivityFailure;
    }

    /// <summary>
    /// Increments the error counter with low-cardinality tags only. Field, arguments, and
    /// caller-supplied dimensions are deliberately absent: they are unbounded, and every distinct
    /// combination is a separate time series to store and query.
    /// </summary>
    private void EmitMetric(Error error) =>
        ErrorCounter.Add(
            1,
            new KeyValuePair<string, object?>(_options.Property("kind"), error.Kind.ToString()),
            new KeyValuePair<string, object?>(_options.Property("code"), error.Code));

    private void WarnIfMetricHasNoListener()
    {
        if (ErrorCounter.Enabled)
            return;

        if (Interlocked.CompareExchange(ref _meterWarned, 1, 0) != 0)
            return;

        _logger.LogWarning(
            "{Counter} is being discarded: call AddMeter({MeterName}).",
            OffsideTelemetry.ErrorCounterName,
            OffsideTelemetry.MeterName);
    }

    private string Message(Error error) =>
        _resolver is null ? error.Code : _resolver.GetMessage(error, _options.Culture);

    private static LogLevel LogLevelFor(DomainErrorSeverity severity) => severity switch
    {
        DomainErrorSeverity.Verbose => LogLevel.Debug,
        DomainErrorSeverity.Information => LogLevel.Information,
        DomainErrorSeverity.Warning => LogLevel.Warning,
        DomainErrorSeverity.Error => LogLevel.Error,
        DomainErrorSeverity.Critical => LogLevel.Critical,
        _ => LogLevel.Error
    };
}
