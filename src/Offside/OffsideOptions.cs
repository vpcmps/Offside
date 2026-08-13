using System.Globalization;
using System.Text;

namespace Offside;

public sealed class OffsideOptions
{
    private readonly List<JsonErrorCatalog> _catalogs = new();

    internal IReadOnlyList<JsonErrorCatalog> Catalogs => _catalogs;

    public OffsideOptions AddJson(CultureInfo culture, string json)
    {
        if (json is null)
            throw new ArgumentNullException(nameof(json));

        return AddJson(culture, new MemoryStream(Encoding.UTF8.GetBytes(json)));
    }

    public OffsideOptions AddJson(CultureInfo culture, Stream json)
    {
        _catalogs.Add(new JsonErrorCatalog(culture, json));
        return this;
    }
}
