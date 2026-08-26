using System.Globalization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Offside.ApplicationInsights;

internal sealed class ApplicationInsightsDomainErrorRecorder : IDomainErrorRecorder
{
    private readonly TelemetryClient _client;
    private readonly OffsideApplicationInsightsOptions _options;
    private readonly IErrorMessageResolver? _resolver;

    public ApplicationInsightsDomainErrorRecorder(
        TelemetryClient client,
        OffsideApplicationInsightsOptions options,
        IErrorMessageResolver? resolver = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _resolver = resolver;
    }

    public void Record(Error error, IReadOnlyDictionary<string, string>? properties = null)
    {
        if (error is null)
            throw new ArgumentNullException(nameof(error));

        var telemetry = new TraceTelemetry(Message(error), _options.SeverityFor(error.Kind));

        if (properties is not null)
        {
            foreach (var pair in properties)
                telemetry.Properties[pair.Key] = pair.Value;
        }

        telemetry.Properties[_options.Property("code")] = error.Code;
        telemetry.Properties[_options.Property("errorCode")] = error.ErrorCode;
        telemetry.Properties[_options.Property("kind")] = error.Kind.ToString();

        if (error.Field is not null)
            telemetry.Properties[_options.Property("field")] = error.Field;

        if (_options.IncludeArguments)
        {
            foreach (var argument in error.Arguments)
            {
                if (argument.Value is null)
                    continue;

                telemetry.Properties[_options.Property("arg." + argument.Key)] =
                    Convert.ToString(argument.Value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }

        _client.TrackTrace(telemetry);
    }

    private string Message(Error error) =>
        _resolver is null ? error.Code : _resolver.GetMessage(error, _options.Culture);
}
