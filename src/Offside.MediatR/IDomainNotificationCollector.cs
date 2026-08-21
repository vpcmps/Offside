namespace Offside.MediatR;

/// <summary>Reads Offside errors collected from MediatR notifications in the current scope.</summary>
/// <remarks>
/// Reads are persistent and never clear the collector. Use one dependency-injection scope per
/// request, message, job, or other logical operation.
/// </remarks>
public interface IDomainNotificationCollector
{
    /// <summary>Gets a value indicating whether the current scope has collected any errors.</summary>
    bool HasNotifications { get; }

    /// <summary>Gets an independent snapshot of the errors collected so far.</summary>
    IReadOnlyList<Error> Errors { get; }

    /// <summary>Creates a successful result when empty, or a failure from the collected errors.</summary>
    /// <returns>A result reflecting the current snapshot.</returns>
    Result ToResult();

    /// <summary>Creates a success with <paramref name="value"/> when empty, or a failure from the collected errors.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="value">The value returned when no notification was collected.</param>
    /// <returns>A result reflecting the current snapshot.</returns>
    Result<T> ToResult<T>(T value);
}
