using System.Globalization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Offside.ApplicationInsights.Tests;

/// <summary>A telemetry client whose channel keeps everything it is given.</summary>
internal sealed class TelemetryHarness : IDisposable
{
    private readonly TelemetryConfiguration _configuration;

    private TelemetryHarness(TelemetryConfiguration configuration, IDomainErrorRecorder recorder, RecordingChannel channel)
    {
        _configuration = configuration;
        Recorder = recorder;
        Channel = channel;
    }

    public IDomainErrorRecorder Recorder { get; }

    public RecordingChannel Channel { get; }

    public IReadOnlyList<TraceTelemetry> Traces => Channel.Sent.OfType<TraceTelemetry>().ToArray();

    public TraceTelemetry SingleTrace() => Assert.Single(Traces);

    public static TelemetryHarness Create(
        Action<OffsideApplicationInsightsOptions>? configure = null,
        IErrorMessageResolver? resolver = null)
    {
        var channel = new RecordingChannel();
        var configuration = new TelemetryConfiguration
        {
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000001",
            TelemetryChannel = channel
        };

        var services = new ServiceCollection()
            .AddSingleton(new TelemetryClient(configuration));

        if (resolver is not null)
            services.AddSingleton(resolver);

        var provider = services
            .AddOffsideApplicationInsights(configure)
            .BuildServiceProvider();

        return new TelemetryHarness(configuration, provider.GetRequiredService<IDomainErrorRecorder>(), channel);
    }

    public void Dispose() => _configuration.Dispose();

    internal sealed class RecordingChannel : ITelemetryChannel
    {
        public List<ITelemetry> Sent { get; } = new();

        public bool? DeveloperMode { get; set; }

        public string? EndpointAddress { get; set; }

        public void Send(ITelemetry item) => Sent.Add(item);

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}

/// <summary>A resolver that echoes a fixed message, so tests can assert what reaches the trace.</summary>
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
