using System.Globalization;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Offside.AspNetCore;
using Offside.FluentValidation;

namespace Offside.FastEndpoint;

/// <summary>
/// Builds an <see cref="OffsideProblem"/> from FluentValidation failures for the FastEndpoints
/// error pipeline.
/// </summary>
public static class OffsideValidationResponse
{
    /// <summary>
    /// Maps <paramref name="failures"/> to Offside errors and renders a problem document
    /// using the request's resolver, culture, and options.
    /// </summary>
    /// <param name="failures">The validation failures FastEndpoints collected.</param>
    /// <param name="httpContext">The current request.</param>
    /// <returns>The problem document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failures"/> or <paramref name="httpContext"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No <see cref="IErrorMessageResolver"/> is registered.</exception>
    public static OffsideProblem Create(
        IReadOnlyList<ValidationFailure> failures,
        HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(failures);
        ArgumentNullException.ThrowIfNull(httpContext);

        var errors = failures.ToOffsideErrors();
        if (errors.Count == 0)
            errors = [Error.Validation("request")];

        var resolver = httpContext.RequestServices.GetRequiredService<IErrorMessageResolver>();
        var options = httpContext.RequestServices.GetService<OffsideAspNetCoreOptions>()
            ?? new OffsideAspNetCoreOptions();

        return OffsideProblemPipeline.Render(errors, resolver, httpContext, options);
    }
}
