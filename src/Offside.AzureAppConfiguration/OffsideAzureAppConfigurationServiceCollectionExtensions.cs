using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Offside.AzureAppConfiguration;

/// <summary>Registration entry point for Azure App Configuration message resolution.</summary>
public static class OffsideAzureAppConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IErrorMessageResolver"/> that resolves messages from the supplied
    /// configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration already populated by the Azure provider.</param>
    /// <param name="configure">Optionally changes the message-section name.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// This method does not configure Azure connections, labels, or refresh. Configure those on
    /// the host, then call this method instead of <c>AddOffside</c>.
    /// </remarks>
    public static IServiceCollection AddOffsideAzureAppConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AzureAppConfigurationOptions>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (configuration is null)
            throw new ArgumentNullException(nameof(configuration));

        var options = new AzureAppConfigurationOptions();
        configure?.Invoke(options);

        var resolver = new ConfigurationErrorMessageResolver(configuration, options.SectionName);
        services.AddSingleton<IErrorMessageResolver>(resolver);
        return services;
    }
}
