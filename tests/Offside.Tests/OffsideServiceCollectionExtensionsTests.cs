using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Offside;
using Xunit;

namespace Offside.Tests;

public sealed class OffsideServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOffside_throws_without_default_catalog()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddOffside(_ => { }));
    }

    [Fact]
    public void AddOffside_registers_resolver()
    {
        var services = new ServiceCollection();
        services.AddOffside(options =>
        {
            options.AddJson(CultureInfo.InvariantCulture, """{ "not_found": "missing" }""");
        });

        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IErrorMessageResolver>();

        Assert.Equal(
            "missing",
            resolver.GetMessage(Error.NotFound("x"), CultureInfo.InvariantCulture));
    }
}
