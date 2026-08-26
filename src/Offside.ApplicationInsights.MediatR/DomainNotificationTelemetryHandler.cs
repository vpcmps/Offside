using global::MediatR;
using Offside.MediatR;

namespace Offside.ApplicationInsights.MediatR;

/// <summary>
/// Records every published <see cref="DomainNotification"/> in Application Insights.
/// </summary>
/// <remarks>
/// Registered by <c>AddOffsideApplicationInsightsMediatR</c>. It runs alongside the collector
/// registered by <c>AddOffsideMediatR</c>; neither replaces the other.
/// </remarks>
public sealed class DomainNotificationTelemetryHandler : INotificationHandler<DomainNotification>
{
    private readonly IDomainErrorRecorder _recorder;

    /// <summary>Initializes the handler.</summary>
    /// <param name="recorder">The recorder each notification is written to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="recorder"/> is <see langword="null"/>.</exception>
    public DomainNotificationTelemetryHandler(IDomainErrorRecorder recorder)
    {
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
    }

    /// <inheritdoc />
    public Task Handle(DomainNotification notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            throw new ArgumentNullException(nameof(notification));

        _recorder.Record(notification.Error);
        return Task.CompletedTask;
    }
}
