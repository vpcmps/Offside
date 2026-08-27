using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Offside.AspNetCore;
using Xunit;

namespace Offside.AspNetCore.Tests;

public sealed class OffsideProblemPipelineTests
{
    [Fact]
    public async Task ServiceUnavailable_is_503_and_does_not_sanitize_reason_out_of_status()
    {
        var result = Result.Failure(Error.ServiceUnavailable("secret-stack"));
        var payload = await ProblemHttpHarness.Execute(result);

        Assert.Equal(503, payload.Status);
        Assert.Equal("ServiceUnavailable", payload.Title);
        Assert.Equal("SERVICE_UNAVAILABLE", payload.ErrorCode);
        Assert.Equal("The service is temporarily unavailable.", payload.Detail);
        Assert.DoesNotContain("secret-stack", payload.Detail);
        Assert.Null(payload.Debug);
    }

    [Fact]
    public async Task Timeout_is_504_and_does_not_use_debug()
    {
        var result = Result.Failure(Error.Timeout("secret-stack"));
        var payload = await ProblemHttpHarness.Execute(result, expose: true);

        Assert.Equal(504, payload.Status);
        Assert.Equal("TIMEOUT", payload.ErrorCode);
        Assert.Equal("The request timed out.", payload.Detail);
        Assert.DoesNotContain("secret-stack", payload.Detail);
        Assert.Null(payload.Debug);
    }

    [Fact]
    public async Task CustomizeProblem_flattens_extension_into_json()
    {
        var options = new OffsideAspNetCoreOptions
        {
            CustomizeProblem = (problem, _) =>
            {
                problem.Extensions["message"] = "legacy";
                problem.Errors[0].Extensions["reason"] = "because";
            }
        };

        var (_, body) = await ProblemHttpHarness.ExecuteRaw(
            Result.Failure(Error.Conflict("order")),
            options);

        using var doc = JsonDocument.Parse(body);
        Assert.Equal("legacy", doc.RootElement.GetProperty("message").GetString());
        Assert.Equal("because", doc.RootElement.GetProperty("errors")[0].GetProperty("reason").GetString());
        Assert.False(doc.RootElement.TryGetProperty("extensions", out _));
    }

    [Fact]
    public async Task CustomizeProblem_reserved_status_key_is_stripped()
    {
        var options = new OffsideAspNetCoreOptions
        {
            CustomizeProblem = (problem, _) => problem.Extensions["status"] = 200
        };

        var (_, body) = await ProblemHttpHarness.ExecuteRaw(
            Result.Failure(Error.Conflict("order")),
            options);

        using var doc = JsonDocument.Parse(body);
        Assert.Equal(409, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task CustomizeProblem_that_throws_still_writes_problem_json()
    {
        var options = new OffsideAspNetCoreOptions
        {
            CustomizeProblem = (_, _) => throw new InvalidOperationException("hook-boom")
        };

        var (http, body) = await ProblemHttpHarness.ExecuteRaw(
            Result.Failure(Error.NotFound("order", 1)),
            options);

        Assert.Equal(404, http.Response.StatusCode);
        Assert.Contains("application/problem+json", http.Response.ContentType);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("NOT_FOUND", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task OnProblem_runs_after_customize_with_http_context()
    {
        HttpContext? seen = null;
        string? seenMessage = null;
        var options = new OffsideAspNetCoreOptions
        {
            CustomizeProblem = (problem, _) => problem.Extensions["message"] = "legacy",
            OnProblem = (problem, _, http) =>
            {
                seen = http;
                seenMessage = problem.Extensions["message"] as string;
            }
        };

        var (http, _) = await ProblemHttpHarness.ExecuteRaw(
            Result.Failure(Error.Conflict("order")),
            options);

        Assert.Same(http, seen);
        Assert.Equal("legacy", seenMessage);
    }

    [Fact]
    public async Task TraceId_default_is_activity_trace_id_hex()
    {
        using var activity = new Activity("offside-test");
        activity.Start();
        try
        {
            var payload = await ProblemHttpHarness.Execute(Result.Failure(Error.NotFound("order", 1)));
            var expected = activity.TraceId.ToString();

            Assert.Equal(expected, payload.TraceId);
            Assert.Equal(32, payload.TraceId!.Length);
            Assert.DoesNotContain('-', payload.TraceId);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public async Task ResolveTraceId_replaces_the_default()
    {
        var options = new OffsideAspNetCoreOptions
        {
            ResolveTraceId = _ => "custom-trace"
        };

        var payload = await ProblemHttpHarness.Execute(
            Result.Failure(Error.NotFound("order", 1)),
            options);

        Assert.Equal("custom-trace", payload.TraceId);
    }

    [Fact]
    public async Task TraceId_falls_back_to_http_trace_identifier_without_activity()
    {
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            var payload = await ProblemHttpHarness.Execute(
                Result.Failure(Error.NotFound("order", 1)),
                new OffsideAspNetCoreOptions(),
                http => http.TraceIdentifier = "host-trace");

            Assert.Equal("host-trace", payload.TraceId);
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    [Fact]
    public async Task LogUnexpected_false_does_not_log()
    {
        var logger = new RecordingLoggerFactory();
        var options = new OffsideAspNetCoreOptions { LogUnexpected = false };
        var services = new ServiceCollection().AddSingleton<ILoggerFactory>(logger).BuildServiceProvider();

        await ProblemHttpHarness.Execute(
            Result.Failure(Error.Unexpected("boom")),
            options,
            http => http.RequestServices = services);

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task LogUnexpected_true_logs_unexpected()
    {
        var logger = new RecordingLoggerFactory();
        var options = new OffsideAspNetCoreOptions { LogUnexpected = true };
        var services = new ServiceCollection().AddSingleton<ILoggerFactory>(logger).BuildServiceProvider();

        await ProblemHttpHarness.Execute(
            Result.Failure(Error.Unexpected("boom")),
            options,
            http => http.RequestServices = services);

        Assert.Contains(logger.Entries, entry => entry.Contains("boom", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ServiceUnavailable_does_not_use_built_in_log()
    {
        var logger = new RecordingLoggerFactory();
        var options = new OffsideAspNetCoreOptions { LogUnexpected = true };
        var services = new ServiceCollection().AddSingleton<ILoggerFactory>(logger).BuildServiceProvider();

        await ProblemHttpHarness.Execute(
            Result.Failure(Error.ServiceUnavailable("otp")),
            options,
            http => http.RequestServices = services);

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Pipeline_records_without_RecordTo_at_the_call_site()
    {
        var recorder = new RecordingRecorder();
        var services = new ServiceCollection().AddSingleton<IDomainErrorRecorder>(recorder).BuildServiceProvider();

        await ProblemHttpHarness.Execute(
            Result.Failure(Error.NotFound("order", 1)),
            new OffsideAspNetCoreOptions(),
            http => http.RequestServices = services);

        var recorded = Assert.Single(recorder.Entries);
        Assert.Equal("not_found", recorded.Error.Code);
        Assert.Equal("404", recorded.Properties!["HttpStatus"]);
    }

    [Fact]
    public async Task Pipeline_merges_TelemetryProperties()
    {
        var recorder = new RecordingRecorder();
        var services = new ServiceCollection().AddSingleton<IDomainErrorRecorder>(recorder).BuildServiceProvider();
        var options = new OffsideAspNetCoreOptions
        {
            TelemetryProperties = (_, _, _) => new Dictionary<string, string> { ["Operation"] = "GetOrder" }
        };

        await ProblemHttpHarness.Execute(
            Result.Failure(Error.Conflict("order")),
            options,
            http => http.RequestServices = services);

        var recorded = Assert.Single(recorder.Entries);
        Assert.Equal("409", recorded.Properties!["HttpStatus"]);
        Assert.Equal("GetOrder", recorded.Properties["Operation"]);
    }

    [Fact]
    public async Task Pipeline_does_not_record_twice_for_one_failure()
    {
        var recorder = new RecordingRecorder();
        var services = new ServiceCollection().AddSingleton<IDomainErrorRecorder>(recorder).BuildServiceProvider();

        await ProblemHttpHarness.Execute(
            Result.Failure(Error.Validation("email"), Error.Validation("name")),
            new OffsideAspNetCoreOptions(),
            http => http.RequestServices = services);

        Assert.Equal(2, recorder.Entries.Count);
    }

    [Fact]
    public async Task LogUnexpected_stays_off_when_a_recorder_is_registered()
    {
        var logger = new RecordingLoggerFactory();
        var recorder = new RecordingRecorder();
        var services = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(logger)
            .AddSingleton<IDomainErrorRecorder>(recorder)
            .BuildServiceProvider();

        await ProblemHttpHarness.Execute(
            Result.Failure(Error.Unexpected("boom")),
            new OffsideAspNetCoreOptions(),
            http => http.RequestServices = services);

        Assert.Empty(logger.Entries);
        Assert.Single(recorder.Entries);
    }

    [Fact]
    public async Task LogUnexpected_explicit_true_still_logs_with_a_recorder()
    {
        var logger = new RecordingLoggerFactory();
        var recorder = new RecordingRecorder();
        var services = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(logger)
            .AddSingleton<IDomainErrorRecorder>(recorder)
            .BuildServiceProvider();

        await ProblemHttpHarness.Execute(
            Result.Failure(Error.Unexpected("boom")),
            new OffsideAspNetCoreOptions { LogUnexpected = true },
            http => http.RequestServices = services);

        Assert.Contains(logger.Entries, entry => entry.Contains("boom", StringComparison.Ordinal));
        Assert.Single(recorder.Entries);
    }

    [Fact]
    public async Task LegacyAliases_add_message_reason_and_technicalDetail()
    {
        var options = new OffsideAspNetCoreOptions
        {
            ExposeExceptionDetails = true,
            LegacyAliases = LegacyProblemAliases.MessageReasonAndTechnicalDetail
        };

        var (_, body) = await ProblemHttpHarness.ExecuteRaw(
            Result.Failure(Error.Unexpected("secret-stack")),
            options);

        using var doc = JsonDocument.Parse(body);
        Assert.Equal("An unexpected error occurred.", doc.RootElement.GetProperty("message").GetString());
        Assert.Equal("secret-stack", doc.RootElement.GetProperty("technicalDetail").GetString());
        Assert.Equal("An unexpected error occurred.", doc.RootElement.GetProperty("errors")[0].GetProperty("reason").GetString());
    }

    [Fact]
    public async Task LegacyAliases_none_omits_the_fields()
    {
        var (_, body) = await ProblemHttpHarness.ExecuteRaw(
            Result.Failure(Error.NotFound("order", 1)),
            new OffsideAspNetCoreOptions());

        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.TryGetProperty("message", out _));
        Assert.False(doc.RootElement.TryGetProperty("technicalDetail", out _));
        Assert.False(doc.RootElement.GetProperty("errors")[0].TryGetProperty("reason", out _));
    }

    [Fact]
    public async Task LegacyAliases_copy_field_to_name()
    {
        var options = new OffsideAspNetCoreOptions
        {
            LegacyAliases = LegacyProblemAliases.MessageReasonAndTechnicalDetail
        };

        var (_, body) = await ProblemHttpHarness.ExecuteRaw(
            Result.Failure(Error.Validation("email")),
            options);

        using var doc = JsonDocument.Parse(body);
        Assert.Equal("email", doc.RootElement.GetProperty("errors")[0].GetProperty("name").GetString());
        Assert.Equal("email", doc.RootElement.GetProperty("errors")[0].GetProperty("reason").GetString());
    }

    [Fact]
    public async Task LegacyAliases_fieldless_error_uses_generalErrors_name()
    {
        var options = new OffsideAspNetCoreOptions
        {
            LegacyAliases = LegacyProblemAliases.MessageReasonAndTechnicalDetail
        };

        var (_, body) = await ProblemHttpHarness.ExecuteRaw(
            Result.Failure(Error.NotFound("order", 1)),
            options);

        using var doc = JsonDocument.Parse(body);
        Assert.Equal("generalErrors", doc.RootElement.GetProperty("errors")[0].GetProperty("name").GetString());
        Assert.False(doc.RootElement.GetProperty("errors")[0].TryGetProperty("field", out var field)
            && field.ValueKind is JsonValueKind.String);
    }

    [Fact]
    public async Task LegacyAliases_empty_general_error_name_omits_name_when_fieldless()
    {
        var options = new OffsideAspNetCoreOptions
        {
            LegacyAliases = LegacyProblemAliases.MessageReasonAndTechnicalDetail,
            LegacyGeneralErrorName = ""
        };

        var (_, body) = await ProblemHttpHarness.ExecuteRaw(
            Result.Failure(Error.Conflict("order")),
            options);

        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.GetProperty("errors")[0].TryGetProperty("name", out _));
    }

    [Fact]
    public async Task LegacyAliases_omit_technicalDetail_when_debug_is_absent()
    {
        var options = new OffsideAspNetCoreOptions
        {
            ExposeExceptionDetails = true,
            LegacyAliases = LegacyProblemAliases.MessageReasonAndTechnicalDetail
        };

        var (_, body) = await ProblemHttpHarness.ExecuteRaw(
            Result.Failure(Error.NotFound("order", 1)),
            options);

        using var doc = JsonDocument.Parse(body);
        Assert.False(doc.RootElement.TryGetProperty("technicalDetail", out _));
        Assert.False(doc.RootElement.TryGetProperty("debug", out _));
    }

    [Fact]
    public async Task RecordMode_primary_error_only_records_once()
    {
        var recorder = new RecordingRecorder();
        var services = new ServiceCollection().AddSingleton<IDomainErrorRecorder>(recorder).BuildServiceProvider();
        var options = new OffsideAspNetCoreOptions { RecordMode = ProblemRecordMode.PrimaryErrorOnly };

        await ProblemHttpHarness.Execute(
            Result.Failure(Error.Validation("email"), Error.Conflict("order"), Error.Validation("name")),
            options,
            http => http.RequestServices = services);

        var recorded = Assert.Single(recorder.Entries);
        Assert.Equal(ErrorKind.Conflict, recorded.Error.Kind);
        Assert.Equal("conflict", recorded.Error.Code);
    }

    private sealed class RecordingRecorder : IDomainErrorRecorder
    {
        public List<(Error Error, IReadOnlyDictionary<string, string>? Properties)> Entries { get; } = [];

        public void Record(Error error, IReadOnlyDictionary<string, string>? properties = null) =>
            Entries.Add((error, properties));
    }

    private sealed class RecordingLoggerFactory : ILoggerFactory, ILogger
    {
        public List<string> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
