using System.Globalization;

namespace Offside;

/// <summary>
/// A single message catalog: the culture it serves, plus the JSON stream mapping
/// <see cref="Error.Code"/> to a message template.
/// </summary>
public sealed class JsonErrorCatalog
{
    /// <summary>Gets the culture this catalog serves. Use <see cref="CultureInfo.InvariantCulture"/> for the required default catalog.</summary>
    public CultureInfo Culture { get; }

    /// <summary>Gets the JSON content, a flat object of <c>"code": "template"</c> pairs.</summary>
    public Stream Json { get; }

    /// <summary>Initializes a new catalog.</summary>
    /// <param name="culture">The culture this catalog serves.</param>
    /// <param name="json">The JSON content. It is read once, when the resolver is constructed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="culture"/> or <paramref name="json"/> is <see langword="null"/>.</exception>
    public JsonErrorCatalog(CultureInfo culture, Stream json)
    {
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
        Json = json ?? throw new ArgumentNullException(nameof(json));
    }
}
