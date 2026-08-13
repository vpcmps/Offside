using System.Collections.ObjectModel;

namespace Offside;

internal static class ErrorArgumentConverter
{
    private static readonly IReadOnlyDictionary<string, object?> Empty =
        new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());

    public static IReadOnlyDictionary<string, object?> ToDictionary(object? arguments)
    {
        if (arguments is null)
            return Empty;

        // Dictionary implements both; copy IDictionary first so we don't alias.
        if (arguments is IDictionary<string, object?> dictionary)
            return Snapshot(dictionary);

        if (arguments is IReadOnlyDictionary<string, object?> readOnly)
            return Snapshot(readOnly);

        return Snapshot(FromAnonymous(arguments));
    }

    private static IEnumerable<KeyValuePair<string, object?>> FromAnonymous(object arguments)
    {
        foreach (var property in arguments.GetType().GetProperties())
        {
            if (property.GetIndexParameters().Length > 0)
                continue;

            yield return new KeyValuePair<string, object?>(
                property.Name,
                property.GetValue(arguments));
        }
    }

    private static IReadOnlyDictionary<string, object?> Snapshot(
        IEnumerable<KeyValuePair<string, object?>> pairs)
    {
        var copy = new Dictionary<string, object?>();
        foreach (var pair in pairs)
            copy[pair.Key] = pair.Value;
        return new ReadOnlyDictionary<string, object?>(copy);
    }
}
