using System.Globalization;
using System.Text;
using Offside;
using Xunit;

namespace Offside.Tests;

public sealed class JsonErrorMessageResolverTests
{
    [Fact]
    public void Falls_back_culture_then_parent_then_default()
    {
        var resolver = Create(
            defaultJson: """{ "not_found": "missing {resource}" }""",
            ("pt", """{ "not_found": "sem {resource}" }"""));

        var error = Error.NotFound("order", 1);
        var message = resolver.GetMessage(error, new CultureInfo("pt-BR"));

        Assert.Equal("sem order", message);
    }

    [Fact]
    public void Missing_key_returns_code()
    {
        var resolver = Create(defaultJson: """{ "conflict": "x" }""");

        var message = resolver.GetMessage(
            Error.Custom("order.already_shipped", ErrorKind.Conflict),
            CultureInfo.InvariantCulture);

        Assert.Equal("order.already_shipped", message);
    }

    [Fact]
    public void Missing_placeholder_leaves_token()
    {
        var resolver = Create(defaultJson: """{ "not_found": "{resource} '{id}' gone" }""");

        var error = Error.NotFound("order");
        var message = resolver.GetMessage(error, CultureInfo.InvariantCulture);

        Assert.Equal("order '{id}' gone", message);
    }

    [Fact]
    public void Constructor_throws_when_default_catalog_missing()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new JsonErrorMessageResolver(Array.Empty<JsonErrorCatalog>()));
    }

    private static JsonErrorMessageResolver Create(
        string defaultJson,
        params (string culture, string json)[] others)
    {
        var catalogs = new List<JsonErrorCatalog>
        {
            new JsonErrorCatalog(CultureInfo.InvariantCulture, Stream(defaultJson))
        };
        foreach (var (culture, json) in others)
            catalogs.Add(new JsonErrorCatalog(new CultureInfo(culture), Stream(json)));

        return new JsonErrorMessageResolver(catalogs);
    }

    private static Stream Stream(string json) =>
        new MemoryStream(Encoding.UTF8.GetBytes(json));
}
