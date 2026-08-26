using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Offside.Refit;

/// <summary>Registration entry point for the Offside Refit integration.</summary>
public static class OffsideRefitServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default mapping options and <see cref="IExternalApiCaller"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optionally adjusts the default mapping options.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// This method does not register Refit clients or <see cref="System.Net.Http.HttpClient"/>.
    /// Configure those in the host, then call this method.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddOffsideRefit(
        this IServiceCollection services,
        Action<OffsideRefitOptions>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var options = new OffsideRefitOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IExternalApiCaller, OffsideRefitCaller>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="OffsideRefitDiagnosticsHandler"/> and the no-op observer it falls back
    /// to. Register your own <see cref="IExternalApiErrorObserver"/> before this call to keep it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// Attach the handler to a client with
    /// <c>.AddHttpMessageHandler&lt;OffsideRefitDiagnosticsHandler&gt;()</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddOffsideRefitDiagnostics(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.AddOffsideRefit();
        services.TryAddSingleton<IExternalApiErrorObserver, NullExternalApiErrorObserver>();
        services.TryAddTransient<OffsideRefitDiagnosticsHandler>();
        return services;
    }
}
