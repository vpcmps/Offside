using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Offside;
using Xunit;

namespace Offside.Tests;

public sealed class OffsideOptionsTests
{
    [Fact]
    public void AddJsonFile_missing_file_names_the_path()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"offside-missing-{Guid.NewGuid():N}.json");
        var options = new OffsideOptions();

        var ex = Assert.Throws<FileNotFoundException>(() =>
            options.AddJsonFile(CultureInfo.InvariantCulture, missing));

        Assert.Contains(missing, ex.Message, StringComparison.Ordinal);
        Assert.Equal(missing, ex.FileName);
    }

    [Fact]
    public void AddJsonFile_loads_catalog()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "default-errors.json");
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddOffside(options => options.AddJsonFile(CultureInfo.InvariantCulture, path));

        var resolver = services.BuildServiceProvider()
            .GetRequiredService<IErrorMessageResolver>();

        var message = resolver.GetMessage(
            Error.ServiceUnavailable("secret-stack"),
            CultureInfo.InvariantCulture);

        Assert.Equal("The service is temporarily unavailable.", message);
        Assert.DoesNotContain("secret-stack", message, StringComparison.Ordinal);
    }

    [Fact]
    public void Default_catalog_does_not_interpolate_reason_for_503_or_504()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "default-errors.json"));
        var catalog = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;

        Assert.Equal("The service is temporarily unavailable.", catalog["service_unavailable"]);
        Assert.Equal("The request timed out.", catalog["timeout"]);
        Assert.DoesNotContain("{reason}", catalog["service_unavailable"], StringComparison.Ordinal);
        Assert.DoesNotContain("{reason}", catalog["timeout"], StringComparison.Ordinal);
    }

    [Fact]
    public void AddJsonFromAssembly_missing_resource_names_the_resource()
    {
        var options = new OffsideOptions();
        var resource = "Offside.Tests.does-not-exist.json";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            options.AddJsonFromAssembly(
                CultureInfo.InvariantCulture,
                typeof(OffsideOptionsTests).Assembly,
                resource));

        Assert.Contains(resource, ex.Message, StringComparison.Ordinal);
        Assert.Contains("Offside.Tests", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddJsonFromAssembly_loads_catalog()
    {
        var resourceName = typeof(OffsideOptionsTests).Assembly
            .GetManifestResourceNames()
            .Single(name => name.EndsWith("test-catalog.json", StringComparison.Ordinal));

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddOffside(options =>
            options.AddJsonFromAssembly(
                CultureInfo.InvariantCulture,
                typeof(OffsideOptionsTests).Assembly,
                resourceName));

        var resolver = services.BuildServiceProvider()
            .GetRequiredService<IErrorMessageResolver>();

        Assert.Equal(
            "missing order",
            resolver.GetMessage(Error.NotFound("order"), CultureInfo.InvariantCulture));
    }
}
