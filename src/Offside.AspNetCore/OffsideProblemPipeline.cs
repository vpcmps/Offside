using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Offside.AspNetCore;

/// <summary>
/// Shared render path for every Offside problem document: culture, trace id, Create,
/// legacy aliases, CustomizeProblem, telemetry, OnProblem, and the built-in Unexpected log.
/// </summary>
internal static class OffsideProblemPipeline
{
    private static readonly HashSet<string> DocumentReservedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "type", "title", "status", "detail", "instance",
        "traceId", "errorCode", "debug", "errors"
    };

    private static readonly HashSet<string> ItemReservedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "code", "errorCode", "kind", "detail", "field"
    };

    internal static OffsideProblem Render(
        IReadOnlyList<Error> errors,
        IErrorMessageResolver resolver,
        HttpContext httpContext,
        OffsideAspNetCoreOptions options,
        CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(options);

        var resolvedCulture = culture ?? ResolveRequestCulture(httpContext);
        var traceId = ResolveTraceId(httpContext, options);
        var problem = OffsideProblem.Create(
            errors,
            resolver,
            resolvedCulture,
            traceId,
            options.ExposeExceptionDetails);

        ApplyLegacyAliases(problem, options);

        InvokeHook(
            httpContext,
            traceId,
            "CustomizeProblem",
            () => options.CustomizeProblem?.Invoke(problem, errors));
        StripReservedKeys(problem);

        Record(httpContext, problem, errors, options);

        InvokeHook(
            httpContext,
            traceId,
            "OnProblem",
            () => options.OnProblem?.Invoke(problem, errors, httpContext));
        StripReservedKeys(problem);

        var recorder = httpContext.RequestServices?.GetService<IDomainErrorRecorder>();
        if (options.ShouldLogUnexpected(recorder is not null))
            TryLogUnexpected(httpContext, errors, traceId);

        return problem;
    }

    internal static string ResolveTraceId(HttpContext httpContext, OffsideAspNetCoreOptions options)
    {
        if (options.ResolveTraceId is not null)
            return options.ResolveTraceId(httpContext);

        var activity = Activity.Current;
        if (activity is not null)
        {
            var traceId = activity.TraceId;
            if (traceId != default)
                return traceId.ToString();
        }

        return httpContext.TraceIdentifier;
    }

    internal static CultureInfo ResolveRequestCulture(HttpContext httpContext)
    {
        var header = httpContext.Request.Headers.AcceptLanguage.ToString();
        if (header.Length == 0)
            return CultureInfo.CurrentUICulture;

        var firstRange = header.Split(',', 2)[0].Split(';', 2)[0].Trim();
        if (firstRange.Length == 0 || firstRange == "*")
            return CultureInfo.CurrentUICulture;

        try
        {
            return CultureInfo.GetCultureInfo(firstRange);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.CurrentUICulture;
        }
    }

    private static void ApplyLegacyAliases(OffsideProblem problem, OffsideAspNetCoreOptions options)
    {
        if ((options.LegacyAliases & LegacyProblemAliases.MessageReasonAndTechnicalDetail) == 0)
            return;

        problem.Extensions["message"] = problem.Detail;

        if (problem.Debug is not null)
            problem.Extensions["technicalDetail"] = problem.Debug;

        foreach (var item in problem.Errors)
        {
            if (item.Field is not null)
                item.Extensions["name"] = item.Field;
            else if (!string.IsNullOrWhiteSpace(options.LegacyGeneralErrorName))
                item.Extensions["name"] = options.LegacyGeneralErrorName;
            item.Extensions["reason"] = item.Detail;
        }
    }

    private static void Record(
        HttpContext httpContext,
        OffsideProblem problem,
        IReadOnlyList<Error> errors,
        OffsideAspNetCoreOptions options)
    {
        var recorder = httpContext.RequestServices?.GetService<IDomainErrorRecorder>();
        if (recorder is null)
            return;

        IReadOnlyDictionary<string, string>? extras = null;
        try
        {
            extras = MergeTelemetryProperties(problem, errors, httpContext, options);
        }
        catch (Exception exception)
        {
            GetLogger(httpContext)?.LogError(
                exception,
                "Offside {Hook} failed. TraceId: {TraceId}",
                "TelemetryProperties",
                problem.TraceId);
        }

        switch (options.RecordMode)
        {
            case ProblemRecordMode.PrimaryErrorOnly:
                recorder.Record(OffsideHttp.SelectPrimary(errors), extras);
                break;
            case ProblemRecordMode.PerError:
                foreach (var error in errors)
                    recorder.Record(error, extras);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    options.RecordMode,
                    "Unknown ProblemRecordMode.");
        }
    }

    private static IReadOnlyDictionary<string, string> MergeTelemetryProperties(
        OffsideProblem problem,
        IReadOnlyList<Error> errors,
        HttpContext httpContext,
        OffsideAspNetCoreOptions options)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["HttpStatus"] = problem.Status.ToString(CultureInfo.InvariantCulture)
        };

        var extra = options.TelemetryProperties?.Invoke(problem, errors, httpContext);
        if (extra is null)
            return properties;

        foreach (var pair in extra)
        {
            if (!properties.ContainsKey(pair.Key))
                properties[pair.Key] = pair.Value;
        }

        return properties;
    }

    private static void InvokeHook(HttpContext httpContext, string traceId, string hook, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            var logger = GetLogger(httpContext);
            logger?.LogError(
                exception,
                "Offside {Hook} failed. TraceId: {TraceId}",
                hook,
                traceId);
        }
    }

    private static void StripReservedKeys(OffsideProblem problem)
    {
        Strip(problem.Extensions, DocumentReservedKeys);
        foreach (var item in problem.Errors)
            Strip(item.Extensions, ItemReservedKeys);
    }

    private static void Strip(IDictionary<string, object?> extensions, HashSet<string> reserved)
    {
        List<string>? remove = null;
        foreach (var key in extensions.Keys)
        {
            if (!reserved.Contains(key))
                continue;
            remove ??= [];
            remove.Add(key);
        }

        if (remove is null)
            return;

        foreach (var key in remove)
            extensions.Remove(key);
    }

    private static void TryLogUnexpected(
        HttpContext httpContext,
        IReadOnlyList<Error> errors,
        string traceId)
    {
        var unexpected = errors.FirstOrDefault(error => error.Kind == ErrorKind.Unexpected);
        if (unexpected is null)
            return;

        var logger = GetLogger(httpContext);
        if (logger is null)
            return;

        unexpected.Arguments.TryGetValue("detail", out var detail);
        logger.LogError(
            "Unexpected error {ErrorCode}. Detail: {Detail}. TraceId: {TraceId}",
            unexpected.Code,
            detail,
            traceId);
    }

    private static ILogger? GetLogger(HttpContext httpContext) =>
        httpContext.RequestServices?.GetService<ILoggerFactory>()
            ?.CreateLogger("Offside.AspNetCore");
}
