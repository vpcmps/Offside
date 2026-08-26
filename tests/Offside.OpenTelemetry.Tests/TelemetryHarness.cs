using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Offside.OpenTelemetry.Tests;

/// <summary>
/// A recorder wired to listeners that keep every signal it produces: log entries, activity events
/// on a live activity, and counter measurements.
/// </summary>
internal sealed class TelemetryHarness : IDisposable
{
    private static readonly ActivitySource Source = new("Offside.OpenTelemetry.Tests");

    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly List<Instrument> _enabledInstruments = new();
    private readonly ServiceProvider _provider;
    private Activity? _activity;

    private TelemetryHarness(
        ServiceProvider provider,
        RecordingLoggerProvider logs,
        ActivityListener activityListener,
        MeterListener meterListener)
    {
        _provider = provider;
        _activityListener = activityListener;
        _meterListener = meterListener;
        Logs = logs;
        Recorder = provider.GetRequiredService<IDomainErrorRecorder>();
    }

    public IDomainErrorRecorder Recorder { get; }

    public RecordingLoggerProvider Logs { get; }

    public List<Measurement> Measurements { get; } = new();

    public Activity? Activity => _activity;

    public static TelemetryHarness Create(
        Action<OffsideOpenTelemetryOptions>? configure = null,
        IErrorMessageResolver? resolver = null,
        bool withActivity = true,
        bool withMeterListener = true)
    {
        var logs = new RecordingLoggerProvider();

        var services = new ServiceCollection()
            .AddLogging(logging => logging
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(logs));

        if (resolver is not null)
            services.AddSingleton(resolver);

        var provider = services
            .AddOffsideOpenTelemetry(configure)
            .BuildServiceProvider();

        var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source == Source,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(activityListener);

        var meterListener = new MeterListener();
        var harness = new TelemetryHarness(provider, logs, activityListener, meterListener);

        if (withMeterListener)
        {
            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name != OffsideTelemetry.MeterName)
                    return;

                listener.EnableMeasurementEvents(instrument);
                harness._enabledInstruments.Add(instrument);
            };
            meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
                harness.Measurements.Add(new Measurement(instrument.Name, value, tags.ToArray())));
            meterListener.Start();
        }

        if (withActivity)
            harness._activity = Source.StartActivity("test");

        return harness;
    }

    public RecordedLog SingleLog() => Assert.Single(Logs.Entries);

    public Measurement SingleMeasurement() => Assert.Single(Measurements);

    public ActivityEvent SingleEvent() => Assert.Single(_activity!.Events.ToArray());

    public void Dispose()
    {
        _activity?.Dispose();
        foreach (var instrument in _enabledInstruments)
            _meterListener.DisableMeasurementEvents(instrument);
        _meterListener.Dispose();
        _activityListener.Dispose();
        _provider.Dispose();
    }

    internal sealed record Measurement(string Instrument, long Value, KeyValuePair<string, object?>[] Tags);
}

/// <summary>One captured log entry, with the state flattened into the dimensions it carried.</summary>
internal sealed record RecordedLog(
    LogLevel Level,
    EventId EventId,
    string Message,
    IReadOnlyList<KeyValuePair<string, object?>> Dimensions)
{
    public string? Dimension(string key) =>
        Dimensions.FirstOrDefault(pair => pair.Key == key).Value as string;

    public bool Has(string key) => Dimensions.Any(pair => pair.Key == key);
}

internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    public List<RecordedLog> Entries { get; } = new();

    public string? LastCategory { get; private set; }

    public ILogger CreateLogger(string categoryName)
    {
        LastCategory = categoryName;
        return new RecordingLogger(this);
    }

    public void Dispose()
    {
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly RecordingLoggerProvider _provider;

        public RecordingLogger(RecordingLoggerProvider provider) => _provider = provider;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // The cast is the point of the test: an exporter only sees dimensions as attributes
            // when the state is a list of key/value pairs.
            var dimensions = state as IReadOnlyList<KeyValuePair<string, object?>>
                ?? Array.Empty<KeyValuePair<string, object?>>();

            _provider.Entries.Add(new RecordedLog(
                logLevel,
                eventId,
                formatter(state, exception),
                dimensions.ToArray()));
        }
    }
}

/// <summary>A resolver that echoes a fixed message, so tests can assert what reaches the log.</summary>
internal sealed class StubMessageResolver : IErrorMessageResolver
{
    private readonly string _message;

    public StubMessageResolver(string message) => _message = message;

    public CultureInfo? LastCulture { get; private set; }

    public string GetMessage(Error error, CultureInfo culture)
    {
        LastCulture = culture;
        return _message;
    }
}
