using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Offside.AspNetCore;

/// <summary>Registers and installs the opt-in Offside exception handler.</summary>
public static class OffsideExceptionHandlerExtensions
{
    /// <summary>Registers the Offside handler and the ASP.NET Core problem details service.</summary>
    public static IServiceCollection AddOffsideExceptionHandler(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddProblemDetails();
        services.AddExceptionHandler<OffsideExceptionHandler>();
        return services;
    }

    /// <summary>Installs exception handling middleware for downstream requests.</summary>
    public static IApplicationBuilder UseOffsideExceptionHandler(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
#if NET10_0_OR_GREATER
        return app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            SuppressDiagnosticsCallback = context => context.Exception is DomainException
        });
#else
        return app.UseExceptionHandler();
#endif
    }
}

internal sealed class OffsideExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
            return false;

        var options = httpContext.RequestServices.GetRequiredService<OffsideAspNetCoreOptions>();
        if (exception is not DomainException && !options.HandleUnexpectedExceptions)
            return false;

        if (cancellationToken.IsCancellationRequested || httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        if (exception is BadHttpRequestException badRequest)
        {
            httpContext.Response.StatusCode = badRequest.StatusCode;
            return true;
        }

        IReadOnlyList<Error> errors = exception is DomainException domain
            ? domain.Errors
            : [Error.Unexpected(exception.ToString())];

        var resolver = httpContext.RequestServices.GetRequiredService<IErrorMessageResolver>();
        var result = new OffsideProblemResult(
            errors, resolver, culture: null, options, skipUnexpectedLog: exception is not DomainException);
        try
        {
            await result.ExecuteAsync(httpContext);
        }
        catch (Exception writeError) when (
            writeError is OperationCanceledException or IOException
            && httpContext.RequestAborted.IsCancellationRequested)
        {
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.Clear();
                httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }
        }
        return true;
    }
}
