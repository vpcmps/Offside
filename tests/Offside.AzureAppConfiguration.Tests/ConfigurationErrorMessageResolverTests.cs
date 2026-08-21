using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Offside;
using Offside.AzureAppConfiguration;
using Xunit;

namespace Offside.AzureAppConfiguration.Tests;

public sealed class ConfigurationErrorMessageResolverTests
{
    [Fact]
    public void Resolves_exact_culture_from_configuration_key()
    {
        var resolver = CreateResolver(new Dictionary<string, string?>
        {
            ["Errors:default:not_found"] = "missing {resource}",
            ["Errors:pt-BR:not_found"] = "nao encontrado {resource}"
        });

        var message = resolver.GetMessage(Error.NotFound("order"), new CultureInfo("pt-BR"));

        Assert.Equal("nao encontrado order", message);
    }

    [Fact]
    public void Resolves_catalog_flattened_from_json_configuration()
    {
        using var json = new MemoryStream(Encoding.UTF8.GetBytes("""
            {
              "Errors": {
                "default": { "not_found": "missing {resource}" },
                "pt-BR": { "not_found": "nao encontrado {resource}" }
              }
            }
            """));
        var configuration = new ConfigurationBuilder().AddJsonStream(json).Build();
        var resolver = new ConfigurationErrorMessageResolver(configuration);

        var message = resolver.GetMessage(Error.NotFound("order"), new CultureInfo("pt-BR"));

        Assert.Equal("nao encontrado order", message);
    }

    [Fact]
    public void Falls_back_to_parent_then_default_catalog()
    {
        var resolver = CreateResolver(new Dictionary<string, string?>
        {
            ["Errors:default:not_found"] = "missing {resource}",
            ["Errors:pt:not_found"] = "ausente {resource}"
        });

        Assert.Equal("ausente order", resolver.GetMessage(Error.NotFound("order"), new CultureInfo("pt-BR")));
        Assert.Equal("missing order", resolver.GetMessage(Error.NotFound("order"), new CultureInfo("es-MX")));
    }

    [Fact]
    public void Missing_message_returns_error_code()
    {
        var resolver = CreateResolver(new Dictionary<string, string?>
        {
            ["Errors:default:not_found"] = "missing {resource}"
        });

        var message = resolver.GetMessage(
            Error.Custom("order.already_shipped", ErrorKind.Conflict),
            CultureInfo.InvariantCulture);

        Assert.Equal("order.already_shipped", message);
    }

    [Fact]
    public void Missing_argument_leaves_template_token_literal()
    {
        var resolver = CreateResolver(new Dictionary<string, string?>
        {
            ["Errors:default:not_found"] = "missing {resource} {id}"
        });

        Assert.Equal("missing order {id}", resolver.GetMessage(Error.NotFound("order"), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Reads_updated_value_after_configuration_reload()
    {
        var source = new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["Errors:default:not_found"] = "before {resource}"
            }
        };
        var configuration = new ConfigurationBuilder().Add(source).Build();
        var resolver = new ConfigurationErrorMessageResolver(configuration);

        configuration["Errors:default:not_found"] = "after {resource}";
        configuration.Reload();

        Assert.Equal("after order", resolver.GetMessage(Error.NotFound("order"), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Constructor_throws_when_default_catalog_is_missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Errors:pt:not_found"] = "ausente {resource}"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new ConfigurationErrorMessageResolver(configuration));
    }

    [Fact]
    public void Registration_uses_configured_section()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CustomErrors:default:not_found"] = "missing {resource}"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddOffsideAzureAppConfiguration(configuration, options => options.SectionName = "CustomErrors");
        var provider = services.BuildServiceProvider();

        Assert.Equal(
            "missing order",
            provider.GetRequiredService<IErrorMessageResolver>()
                .GetMessage(Error.NotFound("order"), CultureInfo.InvariantCulture));
    }

    private static ConfigurationErrorMessageResolver CreateResolver(
        Dictionary<string, string?> values) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
}
