namespace Offside;

/// <summary>
/// Selects which <see cref="Error.Arguments"/> values may be written as telemetry dimensions.
/// </summary>
public static class ErrorArgumentFilter
{
    /// <summary>
    /// Yields arguments whose values are not null. When <paramref name="includeAll"/> is
    /// <see langword="true"/>, every argument is included; otherwise only keys listed in
    /// <paramref name="keys"/> are.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, object?>> Select(
        Error error,
        bool includeAll,
        IReadOnlyCollection<string>? keys)
    {
        if (error is null)
            throw new ArgumentNullException(nameof(error));

        if (!includeAll && (keys is null || keys.Count == 0))
            yield break;

        foreach (var argument in error.Arguments)
        {
            if (argument.Value is null)
                continue;

            if (!includeAll && !Contains(keys!, argument.Key))
                continue;

            yield return argument;
        }
    }

    private static bool Contains(IReadOnlyCollection<string> keys, string key)
    {
        foreach (var candidate in keys)
        {
            if (string.Equals(candidate, key, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
