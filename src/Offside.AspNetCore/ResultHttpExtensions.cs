using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Offside.AspNetCore;

public static class ResultHttpExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IResult ToHttpResult(
        this Result result,
        IErrorMessageResolver resolver,
        bool exposeExceptionDetails = false) =>
        result.ToHttpResult(resolver, culture: null, Options(exposeExceptionDetails));

    public static IResult ToHttpResult(
        this Result result,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool exposeExceptionDetails = false) =>
        result.ToHttpResult(resolver, culture, Options(exposeExceptionDetails));

    public static IResult ToHttpResult(
        this Result result,
        IErrorMessageResolver resolver,
        CultureInfo? culture,
        OffsideAspNetCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(options);
        if (result.IsSuccess)
            return Results.NoContent();

        return new OffsideProblemResult(result.Errors, resolver, culture, options);
    }

    public static IResult ToHttpResult(this Result result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return result.ToHttpResult(
            httpContext.RequestServices.GetRequiredService<IErrorMessageResolver>(),
            culture: null,
            ResolveOptions(httpContext));
    }

    public static IResult ToHttpResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        bool exposeExceptionDetails = false) =>
        result.ToHttpResult(resolver, culture: null, Options(exposeExceptionDetails));

    public static IResult ToHttpResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool exposeExceptionDetails = false) =>
        result.ToHttpResult(resolver, culture, Options(exposeExceptionDetails));

    public static IResult ToHttpResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        CultureInfo? culture,
        OffsideAspNetCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(options);
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return new OffsideProblemResult(result.Errors, resolver, culture, options);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return result.ToHttpResult(
            httpContext.RequestServices.GetRequiredService<IErrorMessageResolver>(),
            culture: null,
            ResolveOptions(httpContext));
    }

    public static IActionResult ToActionResult(
        this Result result,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool exposeExceptionDetails = false) =>
        result.ToActionResult(resolver, culture, Options(exposeExceptionDetails));

    public static IActionResult ToActionResult(
        this Result result,
        IErrorMessageResolver resolver,
        CultureInfo? culture,
        OffsideAspNetCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(options);
        if (result.IsSuccess)
            return new NoContentResult();

        return new OffsideProblemResult(result.Errors, resolver, culture, options);
    }

    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        bool exposeExceptionDetails = false) =>
        result.ToActionResult(resolver, culture: null, Options(exposeExceptionDetails));

    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool exposeExceptionDetails = false) =>
        result.ToActionResult(resolver, culture, Options(exposeExceptionDetails));

    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        CultureInfo? culture,
        OffsideAspNetCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(options);
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return new OffsideProblemResult(result.Errors, resolver, culture, options);
    }

    private static OffsideAspNetCoreOptions Options(bool exposeExceptionDetails) =>
        new() { ExposeExceptionDetails = exposeExceptionDetails };

    private static OffsideAspNetCoreOptions ResolveOptions(HttpContext httpContext) =>
        httpContext.RequestServices.GetService<OffsideAspNetCoreOptions>()
        ?? new OffsideAspNetCoreOptions();

    private sealed class OffsideProblemResult : IResult, IActionResult
    {
        private readonly IReadOnlyList<Error> _errors;
        private readonly IErrorMessageResolver _resolver;
        private readonly CultureInfo? _culture;
        private readonly OffsideAspNetCoreOptions _options;

        public OffsideProblemResult(
            IReadOnlyList<Error> errors,
            IErrorMessageResolver resolver,
            CultureInfo? culture,
            OffsideAspNetCoreOptions options)
        {
            _errors = errors;
            _resolver = resolver;
            _culture = culture;
            _options = options;
        }

        public Task ExecuteResultAsync(ActionContext context) =>
            ExecuteAsync(context.HttpContext);

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var culture = _culture ?? ResolveRequestCulture(httpContext);
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            var problem = OffsideProblem.Create(
                _errors,
                _resolver,
                culture,
                traceId,
                _options.ExposeExceptionDetails);

            if (problem.Status == 500)
                TryLogUnexpected(httpContext, _errors, traceId);

            httpContext.Response.StatusCode = problem.Status;
            httpContext.Response.ContentType = "application/problem+json";
            await JsonSerializer.SerializeAsync(httpContext.Response.Body, problem, SerializerOptions);
        }

        private static void TryLogUnexpected(
            HttpContext httpContext,
            IReadOnlyList<Error> errors,
            string traceId)
        {
            var unexpected = errors.FirstOrDefault(error => error.Kind == ErrorKind.Unexpected);
            if (unexpected is null)
                return;

            var logger = httpContext.RequestServices?.GetService<ILoggerFactory>()
                ?.CreateLogger("Offside.AspNetCore");
            if (logger is null)
                return;

            unexpected.Arguments.TryGetValue("detail", out var detail);
            logger.LogError(
                "Unexpected error {ErrorCode}. Detail: {Detail}. TraceId: {TraceId}",
                unexpected.Code,
                detail,
                traceId);
        }

        private static CultureInfo ResolveRequestCulture(HttpContext httpContext)
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
    }
}
