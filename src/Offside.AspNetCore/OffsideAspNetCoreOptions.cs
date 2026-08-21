using Microsoft.Extensions.Hosting;

namespace Offside.AspNetCore;

/// <summary>
/// Options controlling how Offside renders Problem Details responses.
/// </summary>
public sealed class OffsideAspNetCoreOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the diagnostic detail of an
    /// <see cref="ErrorKind.Unexpected"/> error is echoed back to the client in the
    /// <c>debug</c> field. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// Leave this off in production. The client-facing <c>detail</c> is always the generic
    /// catalog message for <c>unexpected</c>, regardless of this setting; only <c>debug</c>
    /// is gated by it.
    /// </remarks>
    public bool ExposeExceptionDetails { get; set; }

    /// <summary>
    /// Creates options whose <see cref="ExposeExceptionDetails"/> follows
    /// <c>environment.IsDevelopment()</c>.
    /// </summary>
    /// <param name="environment">The host environment.</param>
    /// <returns>The options.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="environment"/> is <see langword="null"/>.</exception>
    public static OffsideAspNetCoreOptions FromEnvironment(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return new OffsideAspNetCoreOptions
        {
            ExposeExceptionDetails = environment.IsDevelopment()
        };
    }
}
