using global::MediatR;

namespace Offside.MediatR;

/// <summary>A MediatR notification that carries one Offside domain error.</summary>
/// <remarks>This is an error notification, not a domain event describing a state change.</remarks>
public sealed class DomainNotification : INotification
{
    /// <summary>Initializes a notification for an error.</summary>
    /// <param name="error">The error to publish.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public DomainNotification(Error error)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>Gets the error carried by this notification.</summary>
    public Error Error { get; }
}
