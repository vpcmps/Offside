using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Offside.OpenTelemetry;

/// <summary>Registration entry point for the Offside OpenTelemetry integration.</summary>
public static class OffsideOpenTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IDomainErrorRecorder"/> on top of the host's logging, activity, and
    /// metrics primitives.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optionally adjusts the telemetry options.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// This method configures neither OpenTelemetry nor an exporter, and reads no connection
    /// string. Set up the pipeline in the host — including
    /// <c>AddMeter(OffsideTelemetry.MeterName)</c>, without which the error counter is dropped —
    /// then call this method. Messages are resolved through the <see cref="IErrorMessageResolver"/>
    /// registered by <c>AddOffside</c>; without one, the error's <see cref="Error.Code"/> is
    /// written instead.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddOffsideOpenTelemetry(
        this IServiceCollection services,
        Action<OffsideOpenTelemetryOptions>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var options = new OffsideOpenTelemetryOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IDomainErrorRecorder>(provider =>
            new OpenTelemetryDomainErrorRecorder(
                provider.GetRequiredService<ILoggerFactory>(),
                provider.GetRequiredService<OffsideOpenTelemetryOptions>(),
                provider.GetService<IErrorMessageResolver>()));

        return services;
    }
}
