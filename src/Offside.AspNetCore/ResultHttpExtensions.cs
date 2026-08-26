using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Offside.AspNetCore;

/// <summary>
/// Converts a <see cref="Result"/> or <see cref="Result{T}"/> into an HTTP response: the value
/// on success, an <see cref="OffsideProblem"/> document on failure.
/// </summary>
/// <remarks>
/// <para>
/// Use the <c>ToHttpResult</c> overloads from Minimal APIs and the <c>ToActionResult</c> overloads
/// from MVC controllers. Success maps to <c>204 No Content</c> for a unit <see cref="Result"/> and
/// to <c>200 OK</c> carrying the value for a <see cref="Result{T}"/>.
/// </para>
/// <para>
/// On failure the status code comes from the most severe <see cref="ErrorKind"/> present, with ties
/// resolved in favour of the first error in the result. Every error is still reported in the
/// <c>errors</c> array.
/// </para>
/// <para>
/// The <c>HttpContext</c> overloads are the recommended form: they take the resolver and the
/// options from request services and derive the culture from the <c>Accept-Language</c> header.
/// </para>
/// </remarks>
public static class ResultHttpExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Converts the result into an <see cref="IResult"/>, resolving messages in <c>CultureInfo.CurrentUICulture</c>.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="exposeExceptionDetails">When <see langword="true"/>, unexpected errors also report their diagnostic detail in <c>debug</c>.</param>
    /// <returns><c>204 No Content</c> on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <see langword="null"/>.</exception>
    [Obsolete("Pass HttpContext or OffsideAspNetCoreOptions so CustomizeProblem, telemetry, and legacy aliases apply. This overload builds empty options.")]
    public static IResult ToHttpResult(
        this Result result,
        IErrorMessageResolver resolver,
        bool exposeExceptionDetails = false) =>
        result.ToHttpResult(resolver, culture: null, Options(exposeExceptionDetails));

    /// <summary>Converts the result into an <see cref="IResult"/>, resolving messages in an explicit culture.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="culture">The culture to resolve messages in.</param>
    /// <param name="exposeExceptionDetails">When <see langword="true"/>, unexpected errors also report their diagnostic detail in <c>debug</c>.</param>
    /// <returns><c>204 No Content</c> on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <see langword="null"/>.</exception>
    [Obsolete("Pass HttpContext or OffsideAspNetCoreOptions so CustomizeProblem, telemetry, and legacy aliases apply. This overload builds empty options.")]
    public static IResult ToHttpResult(
        this Result result,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool exposeExceptionDetails = false) =>
        result.ToHttpResult(resolver, culture, Options(exposeExceptionDetails));

    /// <summary>Converts the result into an <see cref="IResult"/> using explicit options.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="culture">The culture to resolve messages in, or <see langword="null"/> to derive it from the request's <c>Accept-Language</c> header.</param>
    /// <param name="options">Controls whether unexpected errors expose their diagnostic detail.</param>
    /// <returns><c>204 No Content</c> on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Converts the result into an <see cref="IResult"/>, taking everything it needs from the
    /// request: the resolver and options from request services, and the culture from
    /// <c>Accept-Language</c>. This is the recommended overload for Minimal APIs.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="httpContext">The current request.</param>
    /// <returns><c>204 No Content</c> on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="httpContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No <see cref="IErrorMessageResolver"/> is registered. Call <c>AddOffside</c> at startup.</exception>
    /// <example>
    /// <code>
    /// app.MapPost("/orders", (CreateOrder cmd, HttpContext http) =>
    ///     handler.Handle(cmd).ToHttpResult(http));
    /// </code>
    /// </example>
    public static IResult ToHttpResult(this Result result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return result.ToHttpResult(
            httpContext.RequestServices.GetRequiredService<IErrorMessageResolver>(),
            culture: null,
            ResolveOptions(httpContext));
    }

    /// <summary>Converts the result into an <see cref="IResult"/>, resolving messages in <c>CultureInfo.CurrentUICulture</c>.</summary>
    /// <typeparam name="T">The result's value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="exposeExceptionDetails">When <see langword="true"/>, unexpected errors also report their diagnostic detail in <c>debug</c>.</param>
    /// <returns><c>200 OK</c> with the value on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <see langword="null"/>.</exception>
    [Obsolete("Pass HttpContext or OffsideAspNetCoreOptions so CustomizeProblem, telemetry, and legacy aliases apply. This overload builds empty options.")]
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        bool exposeExceptionDetails = false) =>
        result.ToHttpResult(resolver, culture: null, Options(exposeExceptionDetails));

    /// <summary>Converts the result into an <see cref="IResult"/>, resolving messages in an explicit culture.</summary>
    /// <typeparam name="T">The result's value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="culture">The culture to resolve messages in.</param>
    /// <param name="exposeExceptionDetails">When <see langword="true"/>, unexpected errors also report their diagnostic detail in <c>debug</c>.</param>
    /// <returns><c>200 OK</c> with the value on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <see langword="null"/>.</exception>
    [Obsolete("Pass HttpContext or OffsideAspNetCoreOptions so CustomizeProblem, telemetry, and legacy aliases apply. This overload builds empty options.")]
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool exposeExceptionDetails = false) =>
        result.ToHttpResult(resolver, culture, Options(exposeExceptionDetails));

    /// <summary>Converts the result into an <see cref="IResult"/> using explicit options.</summary>
    /// <typeparam name="T">The result's value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="culture">The culture to resolve messages in, or <see langword="null"/> to derive it from the request's <c>Accept-Language</c> header.</param>
    /// <param name="options">Controls whether unexpected errors expose their diagnostic detail.</param>
    /// <returns><c>200 OK</c> with the value on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Converts the result into an <see cref="IResult"/>, taking everything it needs from the
    /// request: the resolver and options from request services, and the culture from
    /// <c>Accept-Language</c>. This is the recommended overload for Minimal APIs.
    /// </summary>
    /// <typeparam name="T">The result's value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="httpContext">The current request.</param>
    /// <returns><c>200 OK</c> with the value on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="httpContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No <see cref="IErrorMessageResolver"/> is registered. Call <c>AddOffside</c> at startup.</exception>
    /// <example>
    /// <code>
    /// app.MapGet("/orders/{id}", (string id, HttpContext http) =>
    ///     orders.Get(id).ToHttpResult(http));
    /// </code>
    /// </example>
    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return result.ToHttpResult(
            httpContext.RequestServices.GetRequiredService<IErrorMessageResolver>(),
            culture: null,
            ResolveOptions(httpContext));
    }

    /// <summary>Converts the result into an <see cref="IActionResult"/> for an MVC controller.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="culture">The culture to resolve messages in.</param>
    /// <param name="exposeExceptionDetails">When <see langword="true"/>, unexpected errors also report their diagnostic detail in <c>debug</c>.</param>
    /// <returns><c>204 No Content</c> on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// A unit <see cref="Result"/> has no culture-less <c>ToActionResult</c> overload — pass a
    /// culture explicitly, or pass <see langword="null"/> through the options overload to fall
    /// back to <c>Accept-Language</c>.
    /// </remarks>
    [Obsolete("Pass OffsideAspNetCoreOptions so CustomizeProblem, telemetry, and legacy aliases apply. This overload builds empty options.")]
    public static IActionResult ToActionResult(
        this Result result,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool exposeExceptionDetails = false) =>
        result.ToActionResult(resolver, culture, Options(exposeExceptionDetails));

    /// <summary>Converts the result into an <see cref="IActionResult"/> using explicit options.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="culture">The culture to resolve messages in, or <see langword="null"/> to derive it from the request's <c>Accept-Language</c> header.</param>
    /// <param name="options">Controls whether unexpected errors expose their diagnostic detail.</param>
    /// <returns><c>204 No Content</c> on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
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

    /// <summary>Converts the result into an <see cref="IActionResult"/>, deriving the culture from the request's <c>Accept-Language</c> header.</summary>
    /// <typeparam name="T">The result's value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="exposeExceptionDetails">When <see langword="true"/>, unexpected errors also report their diagnostic detail in <c>debug</c>.</param>
    /// <returns><c>200 OK</c> with the value on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <see langword="null"/>.</exception>
    [Obsolete("Pass OffsideAspNetCoreOptions so CustomizeProblem, telemetry, and legacy aliases apply. This overload builds empty options.")]
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        bool exposeExceptionDetails = false) =>
        result.ToActionResult(resolver, culture: null, Options(exposeExceptionDetails));

    /// <summary>Converts the result into an <see cref="IActionResult"/>, resolving messages in an explicit culture.</summary>
    /// <typeparam name="T">The result's value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="culture">The culture to resolve messages in.</param>
    /// <param name="exposeExceptionDetails">When <see langword="true"/>, unexpected errors also report their diagnostic detail in <c>debug</c>.</param>
    /// <returns><c>200 OK</c> with the value on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// [HttpGet("{id}")]
    /// public IActionResult Get(string id) => _orders.Get(id).ToActionResult(_resolver, CultureInfo.CurrentUICulture);
    /// </code>
    /// </example>
    [Obsolete("Pass OffsideAspNetCoreOptions so CustomizeProblem, telemetry, and legacy aliases apply. This overload builds empty options.")]
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool exposeExceptionDetails = false) =>
        result.ToActionResult(resolver, culture, Options(exposeExceptionDetails));

    /// <summary>Converts the result into an <see cref="IActionResult"/> using explicit options.</summary>
    /// <typeparam name="T">The result's value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="resolver">Resolves error messages.</param>
    /// <param name="culture">The culture to resolve messages in, or <see langword="null"/> to derive it from the request's <c>Accept-Language</c> header.</param>
    /// <param name="options">Controls whether unexpected errors expose their diagnostic detail.</param>
    /// <returns><c>200 OK</c> with the value on success; otherwise a Problem Details response.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolver"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
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
        ?? throw new InvalidOperationException(
            "OffsideAspNetCoreOptions is not registered. Call AddOffsideAspNetCore at startup.");

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
            var problem = OffsideProblemPipeline.Render(
                _errors,
                _resolver,
                httpContext,
                _options,
                _culture);

            httpContext.Response.StatusCode = problem.Status;
            httpContext.Response.ContentType = "application/problem+json";
            await JsonSerializer.SerializeAsync(httpContext.Response.Body, problem, SerializerOptions);
        }
    }
}
