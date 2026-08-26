using System.Collections;

namespace Offside.OpenTelemetry;

/// <summary>
/// The log state of one recorded error. Implementing
/// <see cref="IReadOnlyList{T}"/> of <see cref="KeyValuePair{TKey,TValue}"/> is what makes the
/// dimensions reach an OpenTelemetry exporter as log attributes rather than collapsing into the
/// formatted string.
/// </summary>
internal readonly struct DomainErrorLogState : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly string _message;
    private readonly KeyValuePair<string, object?>[] _dimensions;

    public DomainErrorLogState(string message, KeyValuePair<string, object?>[] dimensions)
    {
        _message = message;
        _dimensions = dimensions;
    }

    public int Count => _dimensions.Length;

    public KeyValuePair<string, object?> this[int index] => _dimensions[index];

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() =>
        ((IEnumerable<KeyValuePair<string, object?>>)_dimensions).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// The text of the log entry, as
    /// <see cref="OffsideOpenTelemetryOptions.FormatMessage"/> produced it. Whatever the format
    /// leaves out is still queryable: the code travels as the <c>offside.code</c> dimension and as
    /// the event id name regardless.
    /// </summary>
    public override string ToString() => _message;
}
