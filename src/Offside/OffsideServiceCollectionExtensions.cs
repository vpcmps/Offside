using Microsoft.Extensions.DependencyInjection;

namespace Offside;

public static class OffsideServiceCollectionExtensions
{
    public static IServiceCollection AddOffside(
        this IServiceCollection services,
        Action<OffsideOptions> configure)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        var options = new OffsideOptions();
        configure(options);

        var resolver = new JsonErrorMessageResolver(options.Catalogs);
        services.AddSingleton<IErrorMessageResolver>(resolver);
        return services;
    }
}
