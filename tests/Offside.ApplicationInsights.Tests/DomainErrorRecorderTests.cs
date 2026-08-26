using System.Globalization;
using Microsoft.ApplicationInsights.DataContracts;
using Xunit;

namespace Offside.ApplicationInsights.Tests;

public class DomainErrorRecorderTests
{
    [Fact]
    public void An_error_becomes_a_trace_with_the_offside_dimensions()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.Validation("email", "email.invalid", "not-an-email", "EMAIL_INVALID"));

        var trace = harness.SingleTrace();
        Assert.Equal("email.invalid", trace.Properties["offside.code"]);
        Assert.Equal("EMAIL_INVALID", trace.Properties["offside.errorCode"]);
        Assert.Equal("Validation", trace.Properties["offside.kind"]);
        Assert.Equal("email", trace.Properties["offside.field"]);
    }

    [Fact]
    public void An_error_without_a_field_writes_no_field_dimension()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.False(harness.SingleTrace().Properties.ContainsKey("offside.field"));
    }

    [Theory]
    [InlineData(ErrorKind.Unexpected, SeverityLevel.Critical)]
    [InlineData(ErrorKind.ServiceUnavailable, SeverityLevel.Error)]
    [InlineData(ErrorKind.Timeout, SeverityLevel.Error)]
    [InlineData(ErrorKind.Unauthorized, SeverityLevel.Warning)]
    [InlineData(ErrorKind.Conflict, SeverityLevel.Warning)]
    [InlineData(ErrorKind.Gone, SeverityLevel.Warning)]
    [InlineData(ErrorKind.NotFound, SeverityLevel.Information)]
    [InlineData(ErrorKind.Validation, SeverityLevel.Information)]
    [InlineData(ErrorKind.BadRequest, SeverityLevel.Information)]
    public void Each_kind_gets_its_default_severity(ErrorKind kind, SeverityLevel expected)
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.Custom("some.rule", kind));

        Assert.Equal(expected, harness.SingleTrace().SeverityLevel);
    }

    [Fact]
    public void The_severity_map_can_be_replaced()
    {
        using var harness = TelemetryHarness.Create(options => options.SeverityFor = _ => DomainErrorSeverity.Verbose);

        harness.Recorder.Record(Error.Unexpected("boom"));

        Assert.Equal(SeverityLevel.Verbose, harness.SingleTrace().SeverityLevel);
    }

    [Fact]
    public void Arguments_stay_out_of_telemetry_by_default()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.NotFound("order", "cpf-12345678900"));

        Assert.DoesNotContain(harness.SingleTrace().Properties, pair => pair.Key.StartsWith("offside.arg.", StringComparison.Ordinal));
    }

    [Fact]
    public void Arguments_are_written_when_explicitly_enabled()
    {
        using var harness = TelemetryHarness.Create(options => options.IncludeArguments = true);

        harness.Recorder.Record(Error.NotFound("order", 42));

        var trace = harness.SingleTrace();
        Assert.Equal("order", trace.Properties["offside.arg.resource"]);
        Assert.Equal("42", trace.Properties["offside.arg.id"]);
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

        var trace = harness.SingleTrace();
        Assert.Equal("missing-header", trace.Properties["offside.arg.rejectionReason"]);
        Assert.False(trace.Properties.ContainsKey("offside.arg.document"));
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

        var trace = harness.SingleTrace();
        Assert.Equal("missing-header", trace.Properties["offside.arg.rejectionReason"]);
        Assert.Equal("12345678900", trace.Properties["offside.arg.document"]);
    }

    [Fact]
    public void Null_arguments_are_skipped()
    {
        using var harness = TelemetryHarness.Create(options => options.IncludeArguments = true);

        harness.Recorder.Record(Error.NotFound("order"));

        Assert.False(harness.SingleTrace().Properties.ContainsKey("offside.arg.id"));
    }

    [Fact]
    public void The_property_prefix_can_be_changed()
    {
        using var harness = TelemetryHarness.Create(options => options.PropertyPrefix = "domain_");

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("not_found", harness.SingleTrace().Properties["domain_code"]);
    }

    [Fact]
    public void Extra_properties_are_merged_in()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(
            Error.NotFound("order", 42),
            new Dictionary<string, string> { ["tenant"] = "acme" });

        Assert.Equal("acme", harness.SingleTrace().Properties["tenant"]);
    }

    [Fact]
    public void Extra_properties_never_overwrite_an_offside_dimension()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(
            Error.NotFound("order", 42),
            new Dictionary<string, string> { ["offside.kind"] = "Forged" });

        Assert.Equal("NotFound", harness.SingleTrace().Properties["offside.kind"]);
    }

    [Fact]
    public void The_message_comes_from_the_resolver_in_the_configured_culture()
    {
        var resolver = new StubMessageResolver("Order 42 was not found.");
        var culture = new CultureInfo("pt-BR");
        using var harness = TelemetryHarness.Create(options => options.Culture = culture, resolver);

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("Order 42 was not found.", harness.SingleTrace().Message);
        Assert.Equal(culture, resolver.LastCulture);
    }

    [Fact]
    public void Without_a_resolver_the_code_is_written_instead()
    {
        using var harness = TelemetryHarness.Create();

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("not_found", harness.SingleTrace().Message);
    }

    [Fact]
    public void The_code_prefixed_format_puts_the_catalog_code_on_the_line()
    {
        using var harness = TelemetryHarness.Create(
            options => options.FormatMessage = DomainErrorMessageFormat.CodePrefixed,
            new StubMessageResolver("Order not found."));

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("[not_found] Order not found.", harness.SingleTrace().Message);
    }

    [Fact]
    public void The_error_code_prefixed_format_puts_the_screen_identifier_on_the_line()
    {
        using var harness = TelemetryHarness.Create(
            options => options.FormatMessage = DomainErrorMessageFormat.ErrorCodePrefixed,
            new StubMessageResolver("Order not found."));

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("[NOT_FOUND] Order not found.", harness.SingleTrace().Message);
    }

    [Fact]
    public void A_custom_format_receives_the_error_and_the_resolved_message()
    {
        using var harness = TelemetryHarness.Create(
            options => options.FormatMessage = (error, message) => $"{error.Kind}/{error.Code}: {message}",
            new StubMessageResolver("Order not found."));

        harness.Recorder.Record(Error.NotFound("order", 42));

        Assert.Equal("NotFound/not_found: Order not found.", harness.SingleTrace().Message);
    }

    [Fact]
    public void The_format_shapes_the_text_but_never_the_dimensions()
    {
        using var harness = TelemetryHarness.Create(
            options => options.FormatMessage = static (_, _) => "redacted",
            new StubMessageResolver("Order not found."));

        harness.Recorder.Record(Error.NotFound("order", 42));

        var trace = harness.SingleTrace();
        Assert.Equal("redacted", trace.Message);
        Assert.Equal("not_found", trace.Properties["offside.code"]);
        Assert.Equal("NotFound", trace.Properties["offside.kind"]);
    }

    [Fact]
    public void Recording_a_null_error_is_rejected()
    {
        using var harness = TelemetryHarness.Create();

        Assert.Throws<ArgumentNullException>(() => harness.Recorder.Record(null!));
    }
}
