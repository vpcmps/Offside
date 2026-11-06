using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Offside.AspNetCore;

internal sealed class OffsideSuccessResult(
    object? value,
    bool hasData,
    Notice? notice,
    IErrorMessageResolver resolver,
    CultureInfo? culture,
    OffsideAspNetCoreOptions options,
    IResult? httpFallback = null,
    IActionResult? mvcFallback = null) : IResult, IActionResult
{
    internal static bool IsEnabled(HttpContext http, OffsideAspNetCoreOptions options) =>
        http.GetEndpoint()?.Metadata.GetMetadata<OffsideSuccessEnvelopeAttribute>()?.Enabled
        ?? options.SuccessEnvelope;

    internal static string? ResolveMessage(
        Notice? notice, IErrorMessageResolver? resolver, CultureInfo? culture, HttpContext http)
    {
        if (notice is null)
            return null;

        var noticeResolver = http.RequestServices?.GetService<INoticeMessageResolver>()
            ?? resolver as INoticeMessageResolver;
        return noticeResolver?.GetMessage(notice, culture ?? OffsideProblemPipeline.ResolveRequestCulture(http))
            ?? notice.Code;
    }

    public Task ExecuteAsync(HttpContext httpContext) =>
        IsEnabled(httpContext, options)
            ? WriteEnvelopeAsync(httpContext)
            : httpFallback!.ExecuteAsync(httpContext);

    public Task ExecuteResultAsync(ActionContext context) =>
        IsEnabled(context.HttpContext, options)
            ? WriteEnvelopeAsync(context.HttpContext)
            : mvcFallback!.ExecuteResultAsync(context);

    private Task WriteEnvelopeAsync(HttpContext http)
    {
        var fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["traceId"] = OffsideProblemPipeline.ResolveTraceId(http, options)
        };
        if (hasData)
            fields.Add("data", value);
        if (notice is not null)
        {
            fields.Add("message", ResolveMessage(notice, resolver, culture, http));
            fields.Add("code", notice.Code);
        }

        if (options.CustomizeSuccess is not null)
        {
            var custom = new Dictionary<string, object?>(fields, StringComparer.OrdinalIgnoreCase);
            OffsideProblemPipeline.InvokeHook(
                http, (string)fields["traceId"]!, "CustomizeSuccess",
                () => options.CustomizeSuccess(custom, notice, http));
            foreach (var extra in custom)
            {
                if (extra.Key.Equals("traceId", StringComparison.OrdinalIgnoreCase)
                    || extra.Key.Equals("data", StringComparison.OrdinalIgnoreCase)
                    || extra.Key.Equals("message", StringComparison.OrdinalIgnoreCase)
                    || extra.Key.Equals("code", StringComparison.OrdinalIgnoreCase))
                    continue;

                fields.Add(extra.Key, extra.Value);
            }
        }

        http.Response.StatusCode = StatusCodes.Status200OK;
        return http.Response.WriteAsJsonAsync(fields);
    }
}
