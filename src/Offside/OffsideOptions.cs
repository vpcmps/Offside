using System.Globalization;
using System.Text;

namespace Offside;

/// <summary>
/// Collects the message catalogs registered through <c>AddOffside</c>.
/// </summary>
/// <remarks>A catalog for <see cref="CultureInfo.InvariantCulture"/> is required.</remarks>
public sealed class OffsideOptions
{
    private readonly List<JsonErrorCatalog> _catalogs = new();

    internal IReadOnlyList<JsonErrorCatalog> Catalogs => _catalogs;

    /// <summary>Adds a catalog from a JSON string.</summary>
    /// <param name="culture">The culture the catalog serves.</param>
    /// <param name="json">The catalog <em>content</em> — not a file path. Read the file yourself, for example with <c>File.ReadAllText</c>.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public OffsideOptions AddJson(CultureInfo culture, string json)
    {
        if (json is null)
            throw new ArgumentNullException(nameof(json));

        return AddJson(culture, new MemoryStream(Encoding.UTF8.GetBytes(json)));
    }

    /// <summary>Adds a catalog from a stream, for example an embedded resource.</summary>
    /// <param name="culture">The culture the catalog serves.</param>
    /// <param name="json">The catalog content. It is read once, when the resolver is built.</param>
    /// <returns>This instance, for chaining.</returns>
    public OffsideOptions AddJson(CultureInfo culture, Stream json)
    {
        _catalogs.Add(new JsonErrorCatalog(culture, json));
        return this;
    }
}
