using global::MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Offside.MediatR;

/// <summary>Registers the scoped Offside domain-notification collector and its MediatR handler.</summary>
public static class OffsideMediatRServiceCollectionExtensions
{
    /// <summary>Registers the Offside MediatR integration once.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// This method does not call <c>AddMediatR</c> and does not register <see cref="IPublisher"/>.
    /// Configure MediatR in the host, then call this method. Use one scope per logical operation.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddOffsideMediatR(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (services.Any(descriptor =>
            descriptor.ServiceType == typeof(OffsideMediatRRegistrationMarker)))
        {
            return services;
        }

        services.AddSingleton<OffsideMediatRRegistrationMarker>();
        services.AddScoped<DomainNotificationCollector>();
        services.AddScoped<IDomainNotificationCollector>(provider =>
            provider.GetRequiredService<DomainNotificationCollector>());
        services.AddScoped<INotificationHandler<DomainNotification>>(provider =>
            provider.GetRequiredService<DomainNotificationCollector>());

        return services;
    }
}

internal sealed class OffsideMediatRRegistrationMarker;
