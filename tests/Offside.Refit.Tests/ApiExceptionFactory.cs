using System.Net;
using System.Net.Http;
using Refit;

namespace Offside.Refit.Tests;

/// <summary>Builds the exception Refit would throw for a given failed response.</summary>
internal static class ApiExceptionFactory
{
    public const string RequestUri = "https://payments.example/orders/42";

    public static ApiException Create(HttpStatusCode statusCode, string? content = null, string? contentType = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, RequestUri);
        var response = new HttpResponseMessage(statusCode) { RequestMessage = request };

        if (content is not null)
            response.Content = new StringContent(content, System.Text.Encoding.UTF8, contentType ?? "application/problem+json");

        return ApiException
            .Create(request, HttpMethod.Get, response, new RefitSettings())
            .GetAwaiter()
            .GetResult();
    }
}
