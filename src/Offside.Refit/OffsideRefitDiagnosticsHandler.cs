using System.Net.Http;

namespace Offside.Refit;

/// <summary>
/// An <see cref="HttpClient"/> handler that reports every failed response and every transport
/// failure to an <see cref="IExternalApiErrorObserver"/>, then lets the outcome continue unchanged.
/// </summary>
/// <remarks>
/// The handler observes only; it never converts a response into a <see cref="Result"/> — that is
/// <see cref="IExternalApiCaller"/>'s job. The response body is left untouched, so the error it
/// reports comes from the status code alone, without the dependency's problem details.
/// </remarks>
public sealed class OffsideRefitDiagnosticsHandler : DelegatingHandler
{
    private readonly IExternalApiErrorObserver _observer;
    private readonly OffsideRefitOptions _options;

    /// <summary>Initializes the handler.</summary>
    /// <param name="observer">Receives the observed errors.</param>
    /// <param name="options">The mapping options used to build them.</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    public OffsideRefitDiagnosticsHandler(IExternalApiErrorObserver observer, OffsideRefitOptions options)
    {
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _observer.Observe(RefitOffsideExtensions.ApplyInboundStatus(
                    new[]
                    {
                        RefitOffsideExtensions.FromStatus(
                            response.StatusCode,
                            request.RequestUri,
                            response.ReasonPhrase,
                            _options)
                    },
                    _options)[0]);
            }

            return response;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _observer.Observe(RefitOffsideExtensions.Timeout(_options, request.RequestUri, exception.Message));
            throw;
        }
        catch (HttpRequestException exception)
        {
            _observer.Observe(exception.ToOffsideError(_options));
            throw;
        }
    }
}
