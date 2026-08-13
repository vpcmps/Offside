using System.Globalization;
using System.Text.Json.Serialization;

namespace Offside.AspNetCore;

public sealed class OffsideProblem
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public int Status { get; init; }
    public required string Detail { get; init; }
    public required string TraceId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Debug { get; init; }
    public required IReadOnlyList<Item> Errors { get; init; }

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

    public sealed class Item
    {
        public required string Code { get; init; }
        public required string Kind { get; init; }
        public required string Detail { get; init; }
        public string? Field { get; init; }
    }
}
