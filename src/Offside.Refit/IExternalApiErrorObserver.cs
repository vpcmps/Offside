namespace Offside.Refit;

/// <summary>
/// Receives the errors <see cref="OffsideRefitDiagnosticsHandler"/> observes on the wire.
/// </summary>
/// <remarks>
/// This is the seam for telemetry. Offside ships a no-op implementation; a host that also uses
/// <c>Offside.ApplicationInsights</c> registers a small adapter forwarding to
/// <c>IDomainErrorRecorder</c>. Neither package depends on the other.
/// Implementations must not throw: the handler reports failures on the request path.
/// </remarks>
public interface IExternalApiErrorObserver
{
    /// <summary>Called once per observed dependency failure.</summary>
    /// <param name="error">The mapped error, derived from the status code or the transport failure.</param>
    void Observe(Error error);
}

internal sealed class NullExternalApiErrorObserver : IExternalApiErrorObserver
{
    public void Observe(Error error)
    {
    }
}
