namespace Offside;

/// <summary>A catalog message carried by a successful result.</summary>
public sealed class Notice
{
    private Notice(string code, IReadOnlyDictionary<string, object?> arguments)
    {
        Code = code;
        Arguments = arguments;
    }

    /// <summary>The catalog key.</summary>
    public string Code { get; }

    /// <summary>The values used to fill template tokens.</summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    /// <summary>Creates a notice for a catalog key and optional template arguments.</summary>
    public static Notice Create(string code, object? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code must not be empty.", nameof(code));

        return new Notice(code, ErrorArgumentConverter.ToDictionary(arguments));
    }
}
