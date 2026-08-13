namespace Offside;

public sealed class Error : IEquatable<Error>
{
    public string Code { get; }
    public ErrorKind Kind { get; }
    public IReadOnlyDictionary<string, object?> Arguments { get; }
    public string? Field { get; }

    internal Error(
        string code,
        ErrorKind kind,
        IReadOnlyDictionary<string, object?> arguments,
        string? field)
    {
        Code = code;
        Kind = kind;
        Arguments = arguments;
        Field = field;
    }

    public static Error NotFound(string resource, object? id = null) =>
        Create("not_found", ErrorKind.NotFound, new { resource, id });

    public static Error Gone(string resource, object? id = null) =>
        Create("gone", ErrorKind.Gone, new { resource, id });

    public static Error Conflict(string resource, string? reason = null) =>
        Create("conflict", ErrorKind.Conflict, new { resource, reason });

    public static Error Validation(string field, string? code = null, object? attemptedValue = null)
    {
        var resolvedCode = string.IsNullOrWhiteSpace(code) ? "validation" : code!.Trim();
        return new Error(
            resolvedCode,
            ErrorKind.Validation,
            ErrorArgumentConverter.ToDictionary(new { field, attemptedValue }),
            field);
    }

    public static Error BadRequest(string? reason = null) =>
        Create("bad_request", ErrorKind.BadRequest, new { reason });

    public static Error Unauthorized(string? reason = null) =>
        Create("unauthorized", ErrorKind.Unauthorized, new { reason });

    public static Error Forbidden(string? reason = null) =>
        Create("forbidden", ErrorKind.Forbidden, new { reason });

    public static Error PreconditionFailed(string? reason = null) =>
        Create("precondition_failed", ErrorKind.PreconditionFailed, new { reason });

    public static Error Unprocessable(string? reason = null) =>
        Create("unprocessable", ErrorKind.Unprocessable, new { reason });

    public static Error TooManyRequests(string? reason = null) =>
        Create("too_many_requests", ErrorKind.TooManyRequests, new { reason });

    public static Error Unexpected(string? detail = null) =>
        Create("unexpected", ErrorKind.Unexpected, new { detail });

    public static Error Custom(
        string code,
        ErrorKind kind,
        object? arguments = null,
        string? field = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code must not be empty.", nameof(code));

        return new Error(
            code.Trim(),
            kind,
            ErrorArgumentConverter.ToDictionary(arguments),
            field);
    }

    private static Error Create(string code, ErrorKind kind, object arguments) =>
        new Error(code, kind, ErrorArgumentConverter.ToDictionary(arguments), field: null);

    public bool Equals(Error? other) =>
        other is not null
        && Code == other.Code
        && Kind == other.Kind
        && Field == other.Field
        && ArgumentsEqual(Arguments, other.Arguments);

    public override bool Equals(object? obj) => Equals(obj as Error);

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Code, Kind, Field);
        var argumentsHash = 0;
        foreach (var pair in Arguments)
            argumentsHash ^= HashCode.Combine(pair.Key, pair.Value);
        return HashCode.Combine(hash, argumentsHash);
    }

    public static bool operator ==(Error? left, Error? right) => object.Equals(left, right);

    public static bool operator !=(Error? left, Error? right) => !object.Equals(left, right);

    private static bool ArgumentsEqual(
        IReadOnlyDictionary<string, object?> left,
        IReadOnlyDictionary<string, object?> right)
    {
        if (left.Count != right.Count) return false;
        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var value)) return false;
            if (!Equals(pair.Value, value)) return false;
        }
        return true;
    }
}
