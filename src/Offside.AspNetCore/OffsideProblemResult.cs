using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Offside.AspNetCore;

internal sealed class OffsideProblemResult : IResult, IActionResult
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IReadOnlyList<Error> _errors;
    private readonly IErrorMessageResolver _resolver;
    private readonly CultureInfo? _culture;
    private readonly OffsideAspNetCoreOptions _options;
    private readonly bool _skipUnexpectedLog;

    public OffsideProblemResult(
        IReadOnlyList<Error> errors,
        IErrorMessageResolver resolver,
        CultureInfo? culture,
        OffsideAspNetCoreOptions options,
        bool skipUnexpectedLog = false)
    {
        _errors = errors;
        _resolver = resolver;
        _culture = culture;
        _options = options;
        _skipUnexpectedLog = skipUnexpectedLog;
    }

    public Task ExecuteResultAsync(ActionContext context) =>
        ExecuteAsync(context.HttpContext);

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var problem = OffsideProblemPipeline.Render(
            _errors,
            _resolver,
            httpContext,
            _options,
            _culture,
            _skipUnexpectedLog);

        httpContext.Response.StatusCode = problem.Status;
        httpContext.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, problem, SerializerOptions, httpContext.RequestAborted);
    }
}
