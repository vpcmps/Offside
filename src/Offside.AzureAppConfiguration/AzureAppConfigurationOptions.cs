namespace Offside.AzureAppConfiguration;

/// <summary>Configures message lookup through Azure App Configuration.</summary>
public sealed class AzureAppConfigurationOptions
{
    /// <summary>
    /// Gets or sets the configuration section containing messages. The default is <c>Errors</c>.
    /// </summary>
    public string SectionName { get; set; } = "Errors";
}
