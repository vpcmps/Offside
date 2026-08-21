using global::MediatR;

namespace Offside.MediatR;

internal sealed class DomainNotificationCollector :
    IDomainNotificationCollector,
    INotificationHandler<DomainNotification>
{
    private readonly object _gate = new();
    private readonly List<Error> _errors = new();
    private readonly HashSet<DomainNotification> _seenNotifications = new();

    public bool HasNotifications
    {
        get
        {
            lock (_gate)
                return _errors.Count != 0;
        }
    }

    public IReadOnlyList<Error> Errors => Snapshot();

    public Result ToResult()
    {
        var errors = Snapshot();
        return errors.Length == 0 ? Result.Success() : Result.Failure(errors);
    }

    public Result<T> ToResult<T>(T value)
    {
        var errors = Snapshot();
        return errors.Length == 0 ? Result<T>.Success(value) : Result<T>.Failure(errors);
    }

    public Task Handle(DomainNotification notification, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_seenNotifications.Add(notification))
                _errors.Add(notification.Error);
        }

        return Task.CompletedTask;
    }

    private Error[] Snapshot()
    {
        lock (_gate)
            return _errors.ToArray();
    }
}
