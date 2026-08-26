using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Offside.OpenTelemetry.Tests;

public class DomainErrorRecorderTests
{
    [Theory]
    [InlineData(ErrorKind.Unexpected, LogLevel.Critical)]
    [InlineData(ErrorKind.ServiceUnavailable, LogLevel.Error)]
    [InlineData(ErrorKind.Timeout, LogLevel.Error)]
    [InlineData(ErrorKind.Unauthorized, LogLevel.Warning)]
    [InlineData(ErrorKind.Forbidden, LogLevel.Warning)]
    [InlineData(ErrorKind.TooManyRequests, LogLevel.Warning)]
    [InlineData(ErrorKind.Conflict, LogLevel.Warning)]
    [InlineData(ErrorKind.PreconditionFailed, LogLevel.Warning)]
    [InlineData(ErrorKind.Gone, LogLevel.Warning)]
    [InlineData(ErrorKind.Unprocessable, LogLevel.Warning)]
    [InlineData(ErrorKind.NotFound, LogLevel.Information)]
    [InlineData(ErrorKind.Validation, LogLevel.Information)]
    [InlineData(ErrorKind.BadRequest, LogLevel.Information)]
    public void Log_level_follows_the_kind(ErrorKind kind, LogLevel expected)
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(ErrorOf(kind));

        Assert.Equal(expected, harness.SingleLog().Level);
    }

    [Fact]
    public void Every_kind_has_an_explicit_severity()
    {
        using var harness = TelemetryHarness.Create();

        foreach (var kind in Enum.GetValues(typeof(ErrorKind)).Cast<ErrorKind>())
            harness.Recorder.Record(ErrorOf(kind));

        Assert.Equal(
            Enum.GetValues(typeof(ErrorKind)).Length,
            harness.Logs.Entries.Count);
    }

    [Fact]
    public void Dimensions_carry_the_error_identity()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.NotFound("order", 42));

        var log = harness.SingleLog();
        Assert.Equal("not_found", log.Dimension("offside.code"));
        Assert.Equal("NotFound", log.Dimension("offside.kind"));
        Assert.NotNull(log.Dimension("offside.errorCode"));
    }

    [Fact]
    public void Field_is_written_only_when_the_error_has_one()
    {
        using var withField = TelemetryHarness.Create();
        withField.Recorder.Record(Error.Validation("email", "invalid"));
        Assert.Equal("email", withField.SingleLog().Dimension("offside.field"));

        using var withoutField = TelemetryHarness.Create();
        withoutField.Recorder.Record(Error.NotFound("order", 42));
        Assert.False(withoutField.SingleLog().Has("offside.field"));
    }

    [Fact]
    public void Arguments_stay_out_unless_asked_for()
    {
        using var off = TelemetryHarness.Create();
        off.Recorder.Record(Error.NotFound("order", 42));
        Assert.DoesNotContain(off.SingleLog().Dimensions, pair => pair.Key.StartsWith("offside.arg.", StringComparison.Ordinal));

        using var on = TelemetryHarness.Create(options => options.IncludeArguments = true);
        on.Recorder.Record(Error.NotFound("order", 42));
        Assert.Contains(on.SingleLog().Dimensions, pair => pair.Key.StartsWith("offside.arg.", StringComparison.Ordinal));
    }

    [Fact]
    public void An_argument_allowlist_writes_only_the_named_keys()
    {
        var error = Error.Custom(
            "access.rejected",
            ErrorKind.Unauthorized,
            new { rejectionReason = "missing-header", document = "12345678900" });

        using var harness = TelemetryHarness.Create(options =>
            options.IncludeArgumentKeys = new[] { "rejectionReason" });

        harness.Recorder.Record(error);

        var log = harness.SingleLog();
        Assert.Equal("missing-header", log.Dimension("offside.arg.rejectionReason"));
        Assert.False(log.Has("offside.arg.document"));
    }

    [Fact]
    public void IncludeArguments_writes_every_key_and_ignores_the_allowlist()
    {
        var error = Error.Custom(
            "access.rejected",
            ErrorKind.Unauthorized,
            new { rejectionReason = "missing-header", document = "12345678900" });

        using var harness = TelemetryHarness.Create(options =>
        {
            options.IncludeArguments = true;
            options.IncludeArgumentKeys = new[] { "rejectionReason" };
        });

        harness.Recorder.Record(error);

        var log = harness.SingleLog();
        Assert.Equal("missing-header", log.Dimension("offside.arg.rejectionReason"));
        Assert.Equal("12345678900", log.Dimension("offside.arg.document"));
    }

    [Fact]
    public void Operations_severity_logs_not_found_as_warning()
    {
        using var harness = TelemetryHarness.Create(options =>
            options.SeverityFor = DomainErrorSeverityMap.Operations);

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal(LogLevel.Warning, harness.SingleLog().Level);
    }

    [Fact]
    public void Caller_dimensions_are_merged_but_never_shadow_an_offside_one()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(
            Error.NotFound("order", 42),
            new Dictionary<string, string>
            {
                ["tenant"] = "acme",
                ["offside.kind"] = "Forged"
            });

        var log = harness.SingleLog();
        Assert.Equal("acme", log.Dimension("tenant"));
        Assert.Equal("NotFound", log.Dimension("offside.kind"));
        Assert.Single(log.Dimensions, pair => pair.Key == "offside.kind");
    }

    [Fact]
    public void Prefix_is_configurable()
    {
        using var harness = TelemetryHarness.Create(options => options.PropertyPrefix = "app.");

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("not_found", harness.SingleLog().Dimension("app.code"));
    }

    [Fact]
    public void Message_comes_from_the_resolver_in_the_configured_culture()
    {
        var resolver = new StubMessageResolver("Order not found.");
        var culture = new CultureInfo("pt-BR");
        using var harness = TelemetryHarness.Create(options => options.Culture = culture, resolver);

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("Order not found.", harness.SingleLog().Message);
        Assert.Equal(culture, resolver.LastCulture);
    }

    [Fact]
    public void Without_a_resolver_the_code_is_the_message()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.NotFound("order", 42));

        var log = harness.SingleLog();
        Assert.Equal("not_found", log.Message);
        Assert.Equal("not_found", log.EventId.Name);
    }

    [Fact]
    public void The_message_is_the_resolved_text_alone_by_default()
    {
        using var harness = TelemetryHarness.Create(resolver: new StubMessageResolver("Order not found."));

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("Order not found.", harness.SingleLog().Message);
    }

    [Fact]
    public void The_code_prefixed_format_puts_the_catalog_code_on_the_line()
    {
        using var harness = TelemetryHarness.Create(
            options => options.FormatMessage = DomainErrorMessageFormat.CodePrefixed,
            new StubMessageResolver("Order not found."));

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("[not_found] Order not found.", harness.SingleLog().Message);
    }

    [Fact]
    public void The_error_code_prefixed_format_puts_the_screen_identifier_on_the_line()
    {
        using var harness = TelemetryHarness.Create(
            options => options.FormatMessage = DomainErrorMessageFormat.ErrorCodePrefixed,
            new StubMessageResolver("Order not found."));

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("[NOT_FOUND] Order not found.", harness.SingleLog().Message);
    }

    [Fact]
    public void A_custom_format_receives_the_error_and_the_resolved_message()
    {
        using var harness = TelemetryHarness.Create(
            options => options.FormatMessage = (error, message) => $"{error.Kind}/{error.Code}: {message}",
            new StubMessageResolver("Order not found."));

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("NotFound/not_found: Order not found.", harness.SingleLog().Message);
    }

    [Fact]
    public void The_format_shapes_the_line_but_never_the_dimensions()
    {
        using var harness = TelemetryHarness.Create(
            options => options.FormatMessage = static (_, _) => "redacted",
            new StubMessageResolver("Order not found."));

        harness.Recorder.Record(Error.NotFound("order", 42));

        var log = harness.SingleLog();
        Assert.Equal("redacted", log.Message);
        Assert.Equal("not_found", log.Dimension("offside.code"));
        Assert.Equal("NotFound", log.Dimension("offside.kind"));
        Assert.Equal("not_found", log.EventId.Name);
    }

    [Fact]
    public void The_format_does_not_reach_the_span_event_or_the_counter()
    {
        using var harness = TelemetryHarness.Create(
            options => options.FormatMessage = DomainErrorMessageFormat.CodePrefixed,
            new StubMessageResolver("Order not found."));

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal(OffsideTelemetry.ErrorEventName, harness.SingleEvent().Name);
        Assert.Equal(OffsideTelemetry.ErrorCounterName, harness.SingleMeasurement().Instrument);
    }

    [Fact]
    public void Logs_are_written_under_the_documented_category()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal(OffsideTelemetry.LoggerCategory, harness.Logs.LastCategory);
    }

    [Fact]
    public void Recording_null_is_a_programming_error()
    {
        using var harness = TelemetryHarness.Create();

        Assert.Throws<ArgumentNullException>(() => harness.Recorder.Record(null!));
    }

    private static Error ErrorOf(ErrorKind kind) => kind switch
    {
        ErrorKind.Unexpected => Error.Unexpected(),
        ErrorKind.Unauthorized => Error.Unauthorized(),
        ErrorKind.Forbidden => Error.Forbidden(),
        ErrorKind.TooManyRequests => Error.TooManyRequests(),
        ErrorKind.Conflict => Error.Conflict("order"),
        ErrorKind.PreconditionFailed => Error.PreconditionFailed(),
        ErrorKind.Gone => Error.Gone("order", 42),
        ErrorKind.Unprocessable => Error.Unprocessable(),
        ErrorKind.NotFound => Error.NotFound("order", 42),
        ErrorKind.Validation => Error.Validation("email", "invalid"),
        ErrorKind.BadRequest => Error.BadRequest(),
        ErrorKind.ServiceUnavailable => Error.ServiceUnavailable(),
        ErrorKind.Timeout => Error.Timeout(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unmapped kind — extend this switch.")
    };
}
