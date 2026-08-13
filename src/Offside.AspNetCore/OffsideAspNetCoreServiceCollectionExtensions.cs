using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Offside.AspNetCore;

public static class OffsideAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OffsideAspNetCoreOptions"/>. Call Core <c>AddOffside</c> separately
    /// to register catalogs and <see cref="IErrorMessageResolver"/>.
    /// When <see cref="IHostEnvironment"/> is in DI, <see cref="OffsideAspNetCoreOptions.ExposeExceptionDetails"/>
    /// defaults to <c>IsDevelopment()</c>.
    /// </summary>
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
