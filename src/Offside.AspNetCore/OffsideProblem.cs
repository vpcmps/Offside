using System.Globalization;

namespace Offside.AspNetCore;

public sealed class OffsideProblem
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public int Status { get; init; }
    public required string Detail { get; init; }
    public required string TraceId { get; init; }
    public required IReadOnlyList<Item> Errors { get; init; }

    public static OffsideProblem Create(
        IReadOnlyList<Error> errors,
        IErrorMessageResolver resolver,
        CultureInfo culture,
        string traceId)
    {
        var primary = ErrorSeverity.SelectPrimary(errors);
        var status = ErrorSeverity.StatusCode(primary.Kind);

        return new OffsideProblem
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = primary.Kind.ToString(),
            Status = status,
            Detail = resolver.GetMessage(primary, culture),
            TraceId = traceId,
            Errors = errors.Select(error => new Item
            {
                Code = error.Code,
                Kind = error.Kind.ToString(),
                Detail = resolver.GetMessage(error, culture),
                Field = error.Field
            }).ToList()
        };
    }

    public sealed class Item
    {
        public required string Code { get; init; }
        public required string Kind { get; init; }
        public required string Detail { get; init; }
        public string? Field { get; init; }
    }
}
