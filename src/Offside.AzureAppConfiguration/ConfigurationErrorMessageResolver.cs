using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Offside.AzureAppConfiguration;

/// <summary>
/// Resolves error messages from an <see cref="IConfiguration"/> hierarchy populated by Azure
/// App Configuration.
/// </summary>
/// <remarks>
/// The resolver reads configuration on every call. When the host refreshes Azure App
/// Configuration, subsequent resolutions observe the refreshed values.
/// </remarks>
public sealed class ConfigurationErrorMessageResolver : IErrorMessageResolver
{
    private const string DefaultCultureSegment = "default";
    private readonly IConfiguration _configuration;
    private readonly string _sectionName;

    /// <summary>
    /// Initializes a resolver using the <c>Errors</c> configuration section.
    /// </summary>
    /// <param name="configuration">Configuration populated by the host.</param>
    public ConfigurationErrorMessageResolver(IConfiguration configuration)
        : this(configuration, "Errors")
    {
    }

    /// <summary>
    /// Initializes a resolver using a specific configuration section.
    /// </summary>
    /// <param name="configuration">Configuration populated by the host.</param>
    /// <param name="sectionName">The root section containing culture catalogs.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sectionName"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">No default-culture catalog was configured.</exception>
    public ConfigurationErrorMessageResolver(IConfiguration configuration, string sectionName)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrWhiteSpace(sectionName))
            throw new ArgumentException("A configuration section name is required.", nameof(sectionName));

        _sectionName = sectionName;

        if (!_configuration.GetSection(BuildCatalogKey(DefaultCultureSegment)).GetChildren().Any())
            throw new InvalidOperationException("A default error catalog is required.");
    }

    /// <inheritdoc />
    public string GetMessage(Error error, CultureInfo culture)
    {
        if (TryFindTemplate(error.Code, culture, out var template))
            return ErrorMessageTemplate.Interpolate(template, error.Arguments);

        return error.Code;
    }

    private bool TryFindTemplate(string code, CultureInfo culture, out string template)
    {
        if (TryGetTemplate(culture.Name, code, out template))
            return true;

        if (!string.Equals(culture.Parent.Name, culture.Name, StringComparison.Ordinal)
            && TryGetTemplate(culture.Parent.Name, code, out template))
            return true;

        return TryGetTemplate(DefaultCultureSegment, code, out template);
    }

    private bool TryGetTemplate(string cultureName, string code, out string template)
    {
        template = _configuration[BuildCatalogKey(cultureName) + ":" + code]!;
        return template is not null;
    }

    private string BuildCatalogKey(string cultureName) => _sectionName + ":" + cultureName;
}
