using System.Globalization;
using System.Text.Json.Serialization;

namespace Offside.AspNetCore;

/// <summary>
/// The body Offside writes for a failed <see cref="Result"/>: an RFC 7807 Problem Details
/// document extended with <c>traceId</c>, the full <c>errors</c> list, and an optional <c>debug</c>.
/// </summary>
/// <remarks>
/// Serialized as <c>application/problem+json</c> with camelCase property names.
/// </remarks>
/// <example>
/// <code language="json">
/// {
///   "type": "https://httpstatuses.io/409",
///   "title": "Conflict",
///   "status": 409,
///   "detail": "Conflict on order.",
///   "traceId": "00-8a3c...-01",
///   "errors": [
///     { "code": "conflict", "kind": "Conflict", "detail": "Conflict on order.", "field": null }
///   ]
/// }
/// </code>
/// </example>
public sealed class OffsideProblem
{
    /// <summary>Gets the problem type URI, <c>https://httpstatuses.io/{status}</c>.</summary>
    public required string Type { get; init; }

    /// <summary>Gets the primary error's <see cref="ErrorKind"/> as a string, for example <c>Conflict</c>.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the HTTP status code, derived from the most severe kind present.</summary>
    public int Status { get; init; }

    /// <summary>Gets the resolved message of the primary error, sanitized when the primary kind is <see cref="ErrorKind.Unexpected"/>.</summary>
    public required string Detail { get; init; }

    /// <summary>Gets the correlation id: <c>Activity.Current?.Id</c> when available, otherwise <c>HttpContext.TraceIdentifier</c>.</summary>
    public required string TraceId { get; init; }

    /// <summary>
    /// Gets the raw diagnostic detail of an unexpected error. Present only when the response is a
    /// 500 <em>and</em> <see cref="OffsideAspNetCoreOptions.ExposeExceptionDetails"/> is enabled;
    /// omitted from the JSON otherwise.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Debug { get; init; }

    /// <summary>Gets every error in the result, in the order the domain reported them.</summary>
    public required IReadOnlyList<Item> Errors { get; init; }

    /// <summary>
    /// Builds a problem document from a set of errors.
    /// </summary>
    /// <param name="errors">The errors carried by the failed result. The most severe kind wins; ties go to the first error in this list.</param>
    /// <param name="resolver">Resolves each error's message.</param>
    /// <param name="culture">The culture to resolve messages in.</param>
    /// <param name="traceId">The correlation id to report.</param>
    /// <param name="exposeExceptionDetails">When <see langword="true"/>, populates <see cref="Debug"/> for unexpected errors.</param>
    /// <returns>The problem document.</returns>
    public static OffsideProblem Create(
        IReadOnlyList<Error> errors,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        string traceId,
        bool exposeExceptionDetails = false)
    {
        var primary = ErrorSeverity.SelectPrimary(errors);
        var status = ErrorSeverity.StatusCode(primary.Kind);
        var sanitize = primary.Kind == ErrorKind.Unexpected;

        return new OffsideProblem
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = primary.Kind.ToString(),
            Status = status,
            Detail = ResolveClientDetail(primary, resolver, culture, sanitize),
            TraceId = traceId,
            Debug = sanitize && exposeExceptionDetails ? ReadUnexpectedDetail(primary) : null,
            Errors = errors.Select(error => new Item
            {
                Code = error.Code,
                Kind = error.Kind.ToString(),
                Detail = ResolveClientDetail(error, resolver, culture, sanitize),
                Field = error.Field
            }).ToList()
        };
    }

    private static string ResolveClientDetail(
        Error error,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        bool sanitize)
    {
        if (sanitize && error.Kind == ErrorKind.Unexpected)
            return resolver.GetMessage(Error.Unexpected(), culture);

        return resolver.GetMessage(error, culture);
    }

    private static string? ReadUnexpectedDetail(Error error)
    {
        if (!error.Arguments.TryGetValue("detail", out var value) || value is null)
            return null;

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    /// <summary>One entry of the <c>errors</c> array — a single domain error rendered for the wire.</summary>
    public sealed class Item
    {
        /// <summary>Gets the stable error code. This is what clients should branch on.</summary>
        public required string Code { get; init; }

        /// <summary>Gets the <see cref="ErrorKind"/> as a string.</summary>
        public required string Kind { get; init; }

        /// <summary>Gets the resolved message for this error.</summary>
        public required string Detail { get; init; }

        /// <summary>Gets the offending field name, or <see langword="null"/> when the error is not attributable to one.</summary>
        public string? Field { get; init; }
    }
}
