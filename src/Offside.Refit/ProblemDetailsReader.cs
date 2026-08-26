using System.Net;
using System.Text.Json;

namespace Offside.Refit;

/// <summary>
/// Reads an RFC 7807 body from a failed dependency response. Every failure mode degrades to
/// <see langword="null"/> so the caller can fall back to the status-code mapping.
/// </summary>
internal static class ProblemDetailsReader
{
    public static IReadOnlyList<Error>? Read(
        string? content,
        HttpStatusCode statusCode,
        Uri? requestUri,
        OffsideRefitOptions options)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var document = JsonDocument.Parse(content!);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            var root = document.RootElement;
            var fallbackKind = OffsideRefit.Kind(statusCode);
            var topErrorCode = ReadString(root, "errorCode");

            if (root.TryGetProperty("errors", out var errors))
            {
                var mapped = errors.ValueKind switch
                {
                    JsonValueKind.Array => ReadOffsideErrors(errors, fallbackKind, statusCode, requestUri, options),
                    JsonValueKind.Object => ReadValidationErrors(errors, topErrorCode, statusCode, requestUri, options),
                    _ => null
                };

                if (mapped is { Count: > 0 })
                    return mapped;
            }

            var detail = ReadString(root, "detail") ?? ReadString(root, "title");
            if (detail is null && topErrorCode is null)
                return null;

            return new[]
            {
                Build(fallbackKind, options.Code(OffsideRefit.CodeSuffix(fallbackKind)), detail, null, topErrorCode, statusCode, requestUri, options)
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<Error>? ReadOffsideErrors(
        JsonElement errors,
        ErrorKind fallbackKind,
        HttpStatusCode statusCode,
        Uri? requestUri,
        OffsideRefitOptions options)
    {
        var mapped = new List<Error>();

        foreach (var item in errors.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                return null;

            var code = ReadString(item, "code");
            if (code is null)
                return null;

            var kind = ReadKind(item) ?? fallbackKind;
            mapped.Add(Build(
                kind,
                options.Code(code),
                ReadString(item, "detail"),
                ReadString(item, "field"),
                ReadString(item, "errorCode"),
                statusCode,
                requestUri,
                options));
        }

        return mapped;
    }

    private static IReadOnlyList<Error> ReadValidationErrors(
        JsonElement errors,
        string? errorCode,
        HttpStatusCode statusCode,
        Uri? requestUri,
        OffsideRefitOptions options)
    {
        var mapped = new List<Error>();

        foreach (var property in errors.EnumerateObject())
        {
            mapped.Add(Build(
                ErrorKind.Validation,
                options.Code("validation"),
                FirstMessage(property.Value),
                property.Name,
                errorCode,
                statusCode,
                requestUri,
                options));
        }

        return mapped;
    }

    private static Error Build(
        ErrorKind kind,
        string code,
        string? detail,
        string? field,
        string? errorCode,
        HttpStatusCode statusCode,
        Uri? requestUri,
        OffsideRefitOptions options) =>
        Error.Custom(
            code,
            kind,
            new
            {
                api = options.ApiName,
                status = (int)statusCode,
                requestUri = requestUri?.ToString(),
                reason = detail
            },
            field,
            errorCode);

    private static ErrorKind? ReadKind(JsonElement item)
    {
        var kind = ReadString(item, "kind");
        return kind is not null && Enum.TryParse(kind, ignoreCase: true, out ErrorKind parsed)
            ? parsed
            : null;
    }

    private static string? FirstMessage(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Array => value.EnumerateArray().FirstOrDefault().ValueKind == JsonValueKind.String
            ? value.EnumerateArray().First().GetString()
            : null,
        _ => null
    };

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
