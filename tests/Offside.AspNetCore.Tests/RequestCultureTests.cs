using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Offside.AspNetCore;
using Xunit;

namespace Offside.AspNetCore.Tests;

public sealed class RequestCultureTests
{
    [Fact]
    public async Task Uses_AcceptLanguage_culture_for_detail()
    {
        var result = Result.Failure(Error.NotFound("order", 1));
        // catalogs: invariant "missing {resource}", pt "sem {resource}"
        var payload = await Execute(result, acceptLanguage: "pt-BR");
        Assert.Equal("sem order", payload.Detail);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("not-a-culture")]
    public async Task Invalid_or_wildcard_AcceptLanguage_returns_problem_details(
        string acceptLanguage)
    {
        var result = Result.Failure(Error.NotFound("order", 1));
        var payload = await Execute(result, acceptLanguage);

        Assert.Equal(404, payload.Status);
        Assert.Equal("missing order", payload.Detail);
    }

    private static async Task<ProblemPayload> Execute(Result result, string acceptLanguage)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        try
        {
            var resolver = new JsonErrorMessageResolver(
            [
                Catalog(CultureInfo.InvariantCulture, """{ "not_found": "missing {resource}" }"""),
                Catalog(new CultureInfo("pt"), """{ "not_found": "sem {resource}" }""")
            ]);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.AcceptLanguage = acceptLanguage;
            httpContext.Response.Body = new MemoryStream();

            var httpResult = result.ToHttpResult(resolver);
            await httpResult.ExecuteAsync(httpContext);

            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var payload = await JsonSerializer.DeserializeAsync<ProblemPayload>(
                httpContext.Response.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(payload);
            return payload!;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    private static JsonErrorCatalog Catalog(CultureInfo culture, string json) =>
        new(culture, new MemoryStream(Encoding.UTF8.GetBytes(json)));
}
