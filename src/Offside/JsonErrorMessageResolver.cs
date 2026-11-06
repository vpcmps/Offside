using System.Globalization;
using System.Text.Json;

namespace Offside;

/// <summary>
/// Resolves error messages from JSON catalogs, one per culture, with fallback from the specific
/// culture to its parent and finally to the invariant catalog.
/// </summary>
/// <remarks>
/// All catalogs are read and parsed once, in the constructor; nothing touches the streams
/// afterwards. Template tokens of the form <c>{name}</c> are replaced with the matching entry
/// from <see cref="Error.Arguments"/>, formatted with <see cref="CultureInfo.InvariantCulture"/>.
/// A token with no matching argument, or a null one, is left in the text verbatim.
/// </remarks>
public sealed class JsonErrorMessageResolver : IErrorMessageResolver, INoticeMessageResolver
{
    private readonly Dictionary<string, Dictionary<string, string>> _catalogs;

    /// <summary>Initializes a new resolver from a set of catalogs.</summary>
    /// <param name="catalogs">The catalogs. One of them must be for <see cref="CultureInfo.InvariantCulture"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="catalogs"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No invariant-culture catalog was supplied.</exception>
    public JsonErrorMessageResolver(IEnumerable<JsonErrorCatalog> catalogs)
    {
        if (catalogs is null)
            throw new ArgumentNullException(nameof(catalogs));

        _catalogs = new Dictionary<string, Dictionary<string, string>>();

        foreach (var catalog in catalogs)
        {
            using var document = JsonDocument.Parse(catalog.Json);
            _catalogs[catalog.Culture.Name] = CatalogJson.Read(document);
        }

        if (!_catalogs.ContainsKey(string.Empty))
            throw new InvalidOperationException("A default (invariant culture) error catalog is required.");
    }

    /// <summary>
    /// Resolves the message for an error, searching <paramref name="culture"/>, then its parent,
    /// then the invariant catalog.
    /// </summary>
    /// <param name="error">The error to describe.</param>
    /// <param name="culture">The culture to resolve the message in.</param>
    /// <returns>The interpolated message, or <see cref="Error.Code"/> when no catalog defines it.</returns>
    public string GetMessage(Error error, CultureInfo culture) =>
        Resolve(error.Code, error.Arguments, culture);

    /// <inheritdoc />
    public string GetMessage(Notice notice, CultureInfo culture) =>
        Resolve(notice.Code, notice.Arguments, culture);

    private string Resolve(string code, IReadOnlyDictionary<string, object?> arguments, CultureInfo culture)
    {
        if (TryFindTemplate(code, culture, out var template))
            return ErrorMessageTemplate.Interpolate(template, arguments);

        return code;
    }

    private bool TryFindTemplate(string code, CultureInfo culture, out string template)
    {
        if (TryGet(culture.Name, code, out template))
            return true;
        if (TryGet(culture.Parent.Name, code, out template))
            return true;
        return TryGet(string.Empty, code, out template);
    }

    private bool TryGet(string cultureName, string code, out string template)
    {
        template = null!;
        return _catalogs.TryGetValue(cultureName, out var messages)
            && messages.TryGetValue(code, out template!);
    }

}
