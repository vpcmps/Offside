namespace Offside;

internal static class ErrorArgumentConverter
{
    public static IReadOnlyDictionary<string, object?> ToDictionary(object? arguments)
    {
        if (arguments is null)
            return new Dictionary<string, object?>();

        if (arguments is IReadOnlyDictionary<string, object?> readOnly)
            return readOnly;

        if (arguments is IDictionary<string, object?> dictionary)
            return new Dictionary<string, object?>(dictionary);

        return new Dictionary<string, object?>();
    }
}
