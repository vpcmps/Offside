using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        CultureInfo culture,
        bool exposeExceptionDetails = false)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return new OffsideProblemResult(result.Errors, resolver, culture, exposeExceptionDetails);
    }

    public static IResult ToHttpResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool exposeExceptionDetails = false)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return new OffsideProblemResult(result.Errors, resolver, culture, exposeExceptionDetails);
    }

    public static IActionResult ToActionResult(
        this Result result,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool exposeExceptionDetails = false)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return new OffsideProblemResult(result.Errors, resolver, culture, exposeExceptionDetails);
    }

    private sealed class OffsideProblemResult : IResult, IActionResult
    {
        private readonly IReadOnlyList<Error> _errors;
        private readonly IErrorMessageResolver _resolver;
        private readonly CultureInfo _culture;
        private readonly bool _exposeExceptionDetails;

        public OffsideProblemResult(
            IReadOnlyList<Error> errors,
            IErrorMessageResolver resolver,
            CultureInfo culture,
            bool exposeExceptionDetails)
        {
            _errors = errors;
            _resolver = resolver;
            _culture = culture;
            _exposeExceptionDetails = exposeExceptionDetails;
        }

        public Task ExecuteResultAsync(ActionContext context) =>
            ExecuteAsync(context.HttpContext);

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            var problem = OffsideProblem.Create(
                _errors,
                _resolver,
                _culture,
                traceId,
                _exposeExceptionDetails);

            httpContext.Response.StatusCode = problem.Status;
            httpContext.Response.ContentType = "application/problem+json";
            await JsonSerializer.SerializeAsync(httpContext.Response.Body, problem, SerializerOptions);
        }
    }
}
