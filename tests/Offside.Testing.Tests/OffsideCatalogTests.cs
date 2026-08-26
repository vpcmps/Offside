using Offside.Testing;
using Xunit;

namespace Offside.Testing.Tests;

/// <summary>Covers the catalog assertions.</summary>
public class OffsideCatalogTests
{
    private const string DefaultJson =
        "{\"not_found\":\"{resource} {id} was not found\",\"conflict\":\"{resource} is in conflict\"}";

    [Fact]
    public void ShouldDefine_lists_what_the_catalog_actually_defines()
    {
        var catalog = OffsideCatalog.FromJson(DefaultJson, "test-catalog");

        catalog.ShouldDefine("not_found");

        var exception = Assert.Throws<OffsideAssertionException>(() => catalog.ShouldDefine("order.duplicated"));

        Assert.Contains("Expected catalog test-catalog to define \"order.duplicated\"", exception.Message, StringComparison.Ordinal);
        Assert.Contains("[\"conflict\", \"not_found\"]", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldDefineAll_names_every_missing_code_at_once()
    {
        var catalog = OffsideCatalog.FromJson(DefaultJson, "test-catalog");

        var exception = Assert.Throws<OffsideAssertionException>(
            () => catalog.ShouldDefineAll("not_found", "order.duplicated", "order.already_shipped"));

        Assert.Contains("[\"order.already_shipped\", \"order.duplicated\"] are missing.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldResolve_detects_a_token_no_argument_fills()
    {
        var catalog = OffsideCatalog.FromJson(DefaultJson, "test-catalog");

        catalog.ShouldResolve(Error.NotFound("order", 42));

        var exception = Assert.Throws<OffsideAssertionException>(() => catalog.ShouldResolve(Error.NotFound("order")));

        Assert.Contains("still has [\"{id}\"] unfilled", exception.Message, StringComparison.Ordinal);
        Assert.Contains("\"order {id} was not found\"", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldResolve_reports_an_undefined_code()
    {
        var catalog = OffsideCatalog.FromJson(DefaultJson, "test-catalog");

        var exception = Assert.Throws<OffsideAssertionException>(
            () => catalog.ShouldResolve(Error.Validation("email")));

        Assert.Contains("it defines no message for \"validation\".", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldDefineSameCodesAs_reports_both_directions()
    {
        var invariant = OffsideCatalog.FromJson(DefaultJson, "errors.json");
        var translated = OffsideCatalog.FromJson(
            "{\"not_found\":\"não encontrado\",\"extra\":\"sobrando\"}", "errors.pt-BR.json");

        var exception = Assert.Throws<OffsideAssertionException>(
            () => translated.ShouldDefineSameCodesAs(invariant));

        Assert.Contains("it is missing [\"conflict\"]", exception.Message, StringComparison.Ordinal);
        Assert.Contains("it defines [\"extra\"] which the other does not", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldDefineSameCodesAs_passes_for_matching_catalogs()
    {
        var invariant = OffsideCatalog.FromJson(DefaultJson, "errors.json");
        var translated = OffsideCatalog.FromJson(
            "{\"not_found\":\"não encontrado\",\"conflict\":\"em conflito\"}", "errors.pt-BR.json");

        translated.ShouldDefineSameCodesAs(invariant);
    }

    [Fact]
    public void FromFile_names_the_path_it_looked_at()
    {
        var exception = Assert.Throws<OffsideAssertionException>(() => OffsideCatalog.FromFile("no-such-catalog.json"));

        Assert.Contains("no-such-catalog.json", exception.Message, StringComparison.Ordinal);
        Assert.Contains("but the file does not exist.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromJson_fails_with_context_for_malformed_content()
    {
        var exception = Assert.Throws<OffsideAssertionException>(() => OffsideCatalog.FromJson("{ not json", "broken"));

        Assert.Contains("Expected catalog broken to be a flat JSON object", exception.Message, StringComparison.Ordinal);
        Assert.IsType<System.Text.Json.JsonException>(exception.InnerException);
    }

    [Fact]
    public void FromFile_reads_the_shipped_default_catalog()
    {
        var catalog = OffsideCatalog.FromFile(Path.Combine("..", "..", "..", "..", "..", "src", "Offside", "errors.json"));

        Assert.NotEmpty(catalog.Codes);
        catalog.ShouldDefine("not_found");
    }
}
