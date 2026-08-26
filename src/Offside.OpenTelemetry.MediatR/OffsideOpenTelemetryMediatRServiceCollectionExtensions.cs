using global::MediatR;
using Microsoft.Extensions.DependencyInjection;
using Offside.MediatR;

namespace Offside.OpenTelemetry.MediatR;

/// <summary>Registration entry point for the MediatR to OpenTelemetry bridge.</summary>
public static class OffsideOpenTelemetryMediatRServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="DomainNotificationTelemetryHandler"/> once, so published domain
    /// notifications are recorded as telemetry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// Call <c>AddOffsideOpenTelemetry</c> for the recorder and configure MediatR in the host.
    /// This method registers neither.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddOffsideOpenTelemetryMediatR(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (services.Any(descriptor =>
            descriptor.ServiceType == typeof(OffsideOpenTelemetryMediatRRegistrationMarker)))
        {
            return services;
        }

        services.AddSingleton<OffsideOpenTelemetryMediatRRegistrationMarker>();
        services.AddScoped<INotificationHandler<DomainNotification>, DomainNotificationTelemetryHandler>();

        return services;
    }
}

internal sealed class OffsideOpenTelemetryMediatRRegistrationMarker;
