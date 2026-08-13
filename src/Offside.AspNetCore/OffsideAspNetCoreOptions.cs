using Microsoft.Extensions.Hosting;

namespace Offside.AspNetCore;

public sealed class OffsideAspNetCoreOptions
{
    public bool ExposeExceptionDetails { get; set; }

    public static OffsideAspNetCoreOptions FromEnvironment(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return new OffsideAspNetCoreOptions
        {
            ExposeExceptionDetails = environment.IsDevelopment()
        };
    }
}
