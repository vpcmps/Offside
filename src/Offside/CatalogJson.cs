using System.Runtime.CompilerServices;
using System.Text.Json;

[assembly: InternalsVisibleTo("Offside.Testing")]

namespace Offside;

internal static class CatalogJson
{
    internal static Dictionary<string, string> Read(JsonDocument document)
    {
        var messages = new Dictionary<string, string>(StringComparer.Ordinal);
        if (document.RootElement.ValueKind == JsonValueKind.Null)
            return messages;

        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("A message catalog must be a JSON object.");

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (messages.ContainsKey(property.Name))
                throw new JsonException($"Duplicate catalog key '{property.Name}'.");

            messages.Add(property.Name, property.Value.Deserialize<string>()!);
        }

        return messages;
    }
}
