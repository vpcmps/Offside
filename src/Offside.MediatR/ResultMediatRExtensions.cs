using global::MediatR;

namespace Offside.MediatR;

/// <summary>Publishes failed Offside results as MediatR notifications.</summary>
public static class ResultMediatRExtensions
{
    /// <summary>Publishes one <see cref="DomainNotification"/> for each error, in result order.</summary>
    /// <param name="result">The result to inspect.</param>
    /// <param name="publisher">The configured MediatR publisher.</param>
    /// <param name="cancellationToken">Stops publication of remaining errors when cancelled.</param>
    /// <returns>The original result after all errors have been published.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="publisher"/> is <see langword="null"/>.</exception>
    public static async Task<Result> PublishDomainNotificationsAsync(
        this Result result,
        IPublisher publisher,
        CancellationToken cancellationToken = default)
    {
        if (publisher is null)
            throw new ArgumentNullException(nameof(publisher));

        foreach (var error in result.Errors)
        {
            await publisher.Publish(new DomainNotification(error), cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>Publishes one <see cref="DomainNotification"/> for each error, in result order.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="publisher">The configured MediatR publisher.</param>
    /// <param name="cancellationToken">Stops publication of remaining errors when cancelled.</param>
    /// <returns>The original result after all errors have been published.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="publisher"/> is <see langword="null"/>.</exception>
    public static async Task<Result<T>> PublishDomainNotificationsAsync<T>(
        this Result<T> result,
        IPublisher publisher,
        CancellationToken cancellationToken = default)
    {
        if (publisher is null)
            throw new ArgumentNullException(nameof(publisher));

        foreach (var error in result.Errors)
        {
            await publisher.Publish(new DomainNotification(error), cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }
}
