using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Offside.Tests;

public sealed class NoticeTests
{
    [Fact]
    public void Successful_composition_preserves_or_replaces_notice()
    {
        var created = Notice.Create("order.created");
        var updated = Notice.Create("order.updated");
        var result = Result<int>.Success(42, created);
        Assert.Same(created, result.Map(value => value.ToString()).Notice);
        Assert.Same(created, result.Bind(value => Result<int>.Success(value + 1)).Notice);
        Assert.Same(updated, result.Bind(value => Result<int>.Success(value, updated)).Notice);
        Assert.Null(result.Bind(_ => Result<int>.Failure(Error.NotFound("order"))).Notice);
        Assert.Null(Result.Combine(result).Notice);
        Assert.Null(Result.Combine(Result.Success(created)).Notice);
    }

    [Fact]
    public void Notice_resolution_uses_shared_catalog_and_fallback()
    {
        var services = new ServiceCollection();
        services.AddOffside(options => options.AddJson(CultureInfo.InvariantCulture,
            """{"order.created":"Order {id} created."}"""));
        using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<INoticeMessageResolver>();
        Assert.Same(resolver, provider.GetRequiredService<IErrorMessageResolver>());
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        Assert.Equal("Order 42 created.", resolver.GetMessage(
            Notice.Create("order.created", new { id = 42 }), culture));
        Assert.Equal("order.updated", resolver.GetMessage(Notice.Create("order.updated"), culture));
    }

    [Fact]
    public void Duplicate_keys_fail_during_registration()
    {
        Assert.Throws<JsonException>(() => new ServiceCollection().AddOffside(options =>
            options.AddJson(CultureInfo.InvariantCulture, """{"order.created":"one","order.created":"two"}""")));
    }
}
