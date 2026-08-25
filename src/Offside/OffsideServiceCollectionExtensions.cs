using Microsoft.Extensions.DependencyInjection;

namespace Offside;

/// <summary>
/// Registration entry point for the Offside core services.
/// </summary>
public static class OffsideServiceCollectionExtensions
{
    /// <summary>
    /// Registers the configured message catalogs and a singleton
    /// <see cref="IErrorMessageResolver"/> backed by them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Adds the catalogs. A catalog for <see cref="System.Globalization.CultureInfo.InvariantCulture"/> is required.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No invariant-culture catalog was added.</exception>
    /// <remarks>
    /// Catalogs are parsed eagerly, so a malformed or missing catalog fails at startup rather
    /// than on the first request. In an ASP.NET Core host, also call <c>AddOffsideAspNetCore</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddOffside(options =>
    /// {
    ///     options.AddJsonFile(CultureInfo.InvariantCulture, "errors/errors.json");
    ///     options.AddJsonFile(new CultureInfo("pt-BR"), "errors/errors.pt-BR.json");
    /// });
    /// </code>
    /// </example>
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
