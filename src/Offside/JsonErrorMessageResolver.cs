using System.Globalization;
using System.Text.Json;

namespace Offside;

public sealed class JsonErrorMessageResolver : IErrorMessageResolver
{
    private readonly Dictionary<string, Dictionary<string, string>> _catalogs;

    public JsonErrorMessageResolver(IEnumerable<JsonErrorCatalog> catalogs)
    {
        if (catalogs is null)
            throw new ArgumentNullException(nameof(catalogs));

        _catalogs = new Dictionary<string, Dictionary<string, string>>();

        foreach (var catalog in catalogs)
        {
            var messages = JsonSerializer.Deserialize<Dictionary<string, string>>(catalog.Json)
                ?? new Dictionary<string, string>();
            _catalogs[catalog.Culture.Name] = messages;
        }

        if (!_catalogs.ContainsKey(string.Empty))
            throw new InvalidOperationException("A default (invariant culture) error catalog is required.");
    }

    public string GetMessage(Error error, CultureInfo culture)
    {
        if (TryFindTemplate(error.Code, culture, out var template))
            return Interpolate(template, error.Arguments);

        return error.Code;
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

    private static string Interpolate(string template, IReadOnlyDictionary<string, object?> arguments)
    {
        foreach (var pair in arguments)
        {
            if (pair.Value is null)
                continue;

            var token = "{" + pair.Key + "}";
            var value = Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty;
            template = template.Replace(token, value);
        }

        return template;
    }
}
