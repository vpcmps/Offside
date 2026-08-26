using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Offside.ApplicationInsights;

/// <summary>Registration entry point for the Offside Application Insights integration.</summary>
public static class OffsideApplicationInsightsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IDomainErrorRecorder"/> backed by the host's
    /// <see cref="Microsoft.ApplicationInsights.TelemetryClient"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optionally adjusts the telemetry options.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// This method does not call <c>AddApplicationInsightsTelemetry</c> and does not read a
    /// connection string. Configure Application Insights in the host, then call this method.
    /// Messages are resolved through the <see cref="IErrorMessageResolver"/> registered by
    /// <c>AddOffside</c>; without one, the error's <see cref="Error.Code"/> is written instead.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddOffsideApplicationInsights(
        this IServiceCollection services,
        Action<OffsideApplicationInsightsOptions>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var options = new OffsideApplicationInsightsOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IDomainErrorRecorder>(provider =>
            new ApplicationInsightsDomainErrorRecorder(
                provider.GetRequiredService<Microsoft.ApplicationInsights.TelemetryClient>(),
                provider.GetRequiredService<OffsideApplicationInsightsOptions>(),
                provider.GetService<IErrorMessageResolver>()));

        return services;
    }
}
