using global::MediatR;
using Offside.Testing;
using Xunit;

namespace Offside.MediatR.Tests;

public sealed class DomainNotificationPublishingTests
{
    [Fact]
    public void Domain_notification_rejects_null_error()
    {
        Assert.Throws<ArgumentNullException>(() => new DomainNotification(null!));
    }

    [Fact]
    public async Task Success_does_not_publish()
    {
        var publisher = new RecordingPublisher();

        var returned = await Result.Success().PublishDomainNotificationsAsync(publisher);

        returned.ShouldBeSuccess();
        Assert.Empty(publisher.Notifications);
    }

    [Fact]
    public async Task Generic_success_does_not_publish_and_returns_its_value()
    {
        var publisher = new RecordingPublisher();

        var returned = await Result<int>.Success(42)
            .PublishDomainNotificationsAsync(publisher);

        returned.ShouldBeSuccess();
        Assert.Equal(42, returned.Value);
        Assert.Empty(publisher.Notifications);
    }

    [Fact]
    public async Task Failure_publishes_each_error_in_result_order()
    {
        var first = Error.NotFound("order", 1);
        var second = Error.Conflict("order", "locked");
        var result = Result.Failure(first, second);
        var publisher = new RecordingPublisher();

        var returned = await result.PublishDomainNotificationsAsync(publisher);

        Assert.Equal(new[] { first, second }, returned.Errors);
        Assert.Equal(new[] { first, second }, publisher.Notifications.Select(x => x.Error));
    }

    [Fact]
    public async Task Generic_failure_returns_the_original_failure()
    {
        var error = Error.Validation("email");
        var result = Result<int>.Failure(error);
        var publisher = new RecordingPublisher();

        var returned = await result.PublishDomainNotificationsAsync(publisher);

        returned.ShouldBeFailure();
        Assert.Same(error, Assert.Single(returned.Errors));
        Assert.Same(error, Assert.Single(publisher.Notifications).Error);
    }

    [Fact]
    public async Task Null_publisher_is_rejected_before_result_inspection()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Result.Success().PublishDomainNotificationsAsync(null!));

        Assert.Equal("publisher", exception.ParamName);
    }

    [Fact]
    public async Task Cancellation_token_is_forwarded_to_every_publish()
    {
        var first = Error.NotFound("order", 1);
        var second = Error.NotFound("order", 2);
        var publisher = new RecordingPublisher();
        using var source = new CancellationTokenSource();

        await Result.Failure(first, second)
            .PublishDomainNotificationsAsync(publisher, source.Token);

        Assert.Equal(new[] { source.Token, source.Token }, publisher.CancellationTokens);
    }

    [Fact]
    public async Task Handler_failure_stops_publication_and_is_propagated()
    {
        var first = Error.NotFound("order", 1);
        var second = Error.NotFound("order", 2);
        var third = Error.NotFound("order", 3);
        var expected = new InvalidOperationException("handler failed");
        var publisher = new FailingPublisher(expected, failureAt: 2);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Result.Failure(first, second, third).PublishDomainNotificationsAsync(publisher));

        Assert.Same(expected, actual);
        Assert.Equal(new[] { first, second }, publisher.SeenErrors);
    }

    [Fact]
    public async Task Cancellation_stops_publication_and_is_propagated()
    {
        var first = Error.NotFound("order", 1);
        var second = Error.NotFound("order", 2);
        var third = Error.NotFound("order", 3);
        using var source = new CancellationTokenSource();
        var publisher = new CancellingPublisher(source, cancellationAt: 2);

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Result.Failure(first, second, third)
                .PublishDomainNotificationsAsync(publisher, source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(new[] { first, second }, publisher.SeenErrors);
    }

    private sealed class RecordingPublisher : IPublisher
    {
        public List<DomainNotification> Notifications { get; } = new();
        public List<CancellationToken> CancellationTokens { get; } = new();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(Assert.IsType<DomainNotification>(notification));
            CancellationTokens.Add(cancellationToken);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Notifications.Add(Assert.IsType<DomainNotification>(notification));
            CancellationTokens.Add(cancellationToken);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingPublisher : IPublisher
    {
        private readonly Exception _exception;
        private readonly int _failureAt;

        public FailingPublisher(Exception exception, int failureAt)
        {
            _exception = exception;
            _failureAt = failureAt;
        }

        public List<Error> SeenErrors { get; } = new();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Publish((DomainNotification)notification, cancellationToken);

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            var domainNotification = Assert.IsType<DomainNotification>(notification);
            SeenErrors.Add(domainNotification.Error);
            return SeenErrors.Count == _failureAt
                ? Task.FromException(_exception)
                : Task.CompletedTask;
        }
    }

    private sealed class CancellingPublisher : IPublisher
    {
        private readonly CancellationTokenSource _source;
        private readonly int _cancellationAt;

        public CancellingPublisher(CancellationTokenSource source, int cancellationAt)
        {
            _source = source;
            _cancellationAt = cancellationAt;
        }

        public List<Error> SeenErrors { get; } = new();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Publish((DomainNotification)notification, cancellationToken);

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            var domainNotification = Assert.IsType<DomainNotification>(notification);
            SeenErrors.Add(domainNotification.Error);
            if (SeenErrors.Count != _cancellationAt)
                return Task.CompletedTask;

            _source.Cancel();
            return Task.FromCanceled(cancellationToken);
        }
    }
}
