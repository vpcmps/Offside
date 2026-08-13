using System.Globalization;

namespace Offside;

public sealed class JsonErrorCatalog
{
    public CultureInfo Culture { get; }
    public Stream Json { get; }

    public JsonErrorCatalog(CultureInfo culture, Stream json)
    {
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
        Json = json ?? throw new ArgumentNullException(nameof(json));
    }
}
