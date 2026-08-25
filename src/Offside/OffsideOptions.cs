using System.Globalization;
using System.Reflection;
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
    /// <param name="json">The catalog <em>content</em> — not a file path. Prefer <see cref="AddJsonFile"/> when the catalog lives on disk.</param>
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

    /// <summary>
    /// Adds a catalog by reading <paramref name="path"/>. Relative paths are resolved against
    /// <see cref="AppContext.BaseDirectory"/>. The file is copied into memory so the catalog
    /// can be parsed later without depending on the original stream position.
    /// </summary>
    /// <param name="culture">The culture the catalog serves.</param>
    /// <param name="path">A file path. Relative paths are combined with the application base directory.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="FileNotFoundException">The resolved file does not exist. The exception names that path.</exception>
    public OffsideOptions AddJsonFile(CultureInfo culture, string path)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        var resolved = Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(resolved))
            throw new FileNotFoundException($"Error catalog file '{resolved}' was not found.", resolved);

        return AddJson(culture, new MemoryStream(File.ReadAllBytes(resolved)));
    }

    /// <summary>
    /// Adds a catalog from an embedded resource. The resource is copied into memory so the
    /// catalog can be parsed later without depending on the original stream position.
    /// </summary>
    /// <param name="culture">The culture the catalog serves.</param>
    /// <param name="assembly">The assembly that contains the resource.</param>
    /// <param name="resourceName">The fully qualified manifest resource name.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> or <paramref name="resourceName"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No resource with that name exists in the assembly. The exception names the resource.</exception>
    public OffsideOptions AddJsonFromAssembly(CultureInfo culture, Assembly assembly, string resourceName)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));
        if (resourceName is null)
            throw new ArgumentNullException(nameof(resourceName));

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            var assemblyName = assembly.GetName().Name ?? assembly.FullName;
            throw new InvalidOperationException(
                $"Error catalog resource '{resourceName}' was not found in assembly '{assemblyName}'.");
        }

        var copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.Position = 0;
        return AddJson(culture, copy);
    }
}
