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
