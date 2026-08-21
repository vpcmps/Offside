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
    /// defaults to <c>IsDevelopment()</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddOffsideAspNetCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(sp =>
        {
            var environment = sp.GetService<IHostEnvironment>();
            return environment is null
                ? new OffsideAspNetCoreOptions()
                : OffsideAspNetCoreOptions.FromEnvironment(environment);
        });
        return services;
    }
}
