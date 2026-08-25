using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Offside.AspNetCore;

/// <summary>
/// Registration entry point for the ASP.NET Core integration.
/// </summary>
public static class OffsideAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OffsideAspNetCoreOptions"/>. Call Core <c>AddOffside</c> separately
    /// to register catalogs and <see cref="IErrorMessageResolver"/>.
    /// When <see cref="IHostEnvironment"/> is in DI, <see cref="OffsideAspNetCoreOptions.ExposeExceptionDetails"/>
    /// defaults to <c>IsDevelopment()</c>. <paramref name="configure"/> runs afterwards and wins.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional extra configuration applied after environment defaults.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddOffsideAspNetCore(
        this IServiceCollection services,
        Action<OffsideAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(sp =>
        {
            var environment = sp.GetService<IHostEnvironment>();
            var options = environment is null
                ? new OffsideAspNetCoreOptions()
                : OffsideAspNetCoreOptions.FromEnvironment(environment);
            configure?.Invoke(options);
            return options;
        });
        return services;
    }
}
