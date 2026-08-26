using System.Globalization;
using Offside.Testing;
using Xunit;

namespace Offside.Testing.Tests;

/// <summary>Covers the refinements chained onto a located error.</summary>
public class ErrorAssertionTests
{
    private static Result Failing(params Error[] errors) => Result.Failure(errors);

    [Fact]
    public void WithKind_reports_the_actual_kind()
    {
        var result = Failing(Error.Conflict("order", "already shipped"));

        var exception = Assert.Throws<OffsideAssertionException>(
            () => result.ShouldHaveError("conflict").WithKind(ErrorKind.Validation));

        Assert.Contains("Expected error \"conflict\"", exception.Message, StringComparison.Ordinal);
        Assert.Contains("to have kind Validation, but found kind Conflict.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithErrorCode_reports_the_actual_error_code()
    {
        var result = Failing(Error.NotFound("order", 42));

        var exception = Assert.Throws<OffsideAssertionException>(
            () => result.ShouldHaveError("not_found").WithErrorCode("ORDER-404"));

        Assert.Contains("to have errorCode \"ORDER-404\", but found errorCode \"NOT_FOUND\".", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ForField_passes_and_reports_null_unambiguously()
    {
        Failing(Error.Validation("email")).ShouldHaveError("validation").ForField("email");

        var exception = Assert.Throws<OffsideAssertionException>(
            () => Failing(Error.NotFound("order")).ShouldHaveError("not_found").ForField("id"));

        Assert.Contains("to have field \"id\", but found field null.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithArgument_distinguishes_a_missing_argument_from_a_different_value()
    {
        var result = Failing(Error.NotFound("order", 42));

        result.ShouldHaveError("not_found").WithArgument("resource", "order");

        var wrongValue = Assert.Throws<OffsideAssertionException>(
            () => result.ShouldHaveError("not_found").WithArgument("resource", "invoice"));
        Assert.Contains("to have argument resource=\"invoice\", but found argument resource=\"order\".", wrongValue.Message, StringComparison.Ordinal);

        var missing = Assert.Throws<OffsideAssertionException>(
            () => result.ShouldHaveError("not_found").WithArgument("customer", "acme"));
        Assert.Contains("but found arguments {", missing.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithMessage_resolves_through_the_supplied_resolver()
    {
        var resolver = Resolver("{\"not_found\":\"{resource} {id} was not found\"}");
        var result = Failing(Error.NotFound("order", 42));

        result.ShouldHaveError("not_found").WithMessage(resolver, "order 42 was not found");

        var exception = Assert.Throws<OffsideAssertionException>(
            () => result.ShouldHaveError("not_found").WithMessage(resolver, "no such order"));

        Assert.Contains("to have message \"no such order\", but found message \"order 42 was not found\".", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithMessage_honours_the_requested_culture()
    {
        var catalogs = new[]
        {
            new JsonErrorCatalog(CultureInfo.InvariantCulture, Stream("{\"not_found\":\"not found\"}")),
            new JsonErrorCatalog(new CultureInfo("pt-BR"), Stream("{\"not_found\":\"não encontrado\"}"))
        };
        var resolver = new JsonErrorMessageResolver(catalogs);
        var result = Failing(Error.NotFound("order"));

        result.ShouldHaveError("not_found").WithMessage(resolver, new CultureInfo("pt-BR"), "não encontrado");
        result.ShouldHaveError("not_found").WithMessage(resolver, "not found");
    }

    [Fact]
    public void And_returns_the_original_result()
    {
        var result = Failing(Error.Validation("email"), Error.Conflict("order"));

        var back = result.ShouldHaveError("validation").And;

        Assert.Equal(2, back.Errors.Count);
    }

    private static IErrorMessageResolver Resolver(string json) =>
        new JsonErrorMessageResolver(new[] { new JsonErrorCatalog(CultureInfo.InvariantCulture, Stream(json)) });

    private static MemoryStream Stream(string json) =>
        new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
}
