using System.Net.Http;
using global::Refit;

namespace Offside.Refit;

internal sealed class OffsideRefitCaller : IExternalApiCaller
{
    private readonly OffsideRefitOptions _defaults;

    public OffsideRefitCaller(OffsideRefitOptions defaults)
    {
        _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
    }

    public async Task<Result<T>> CallAsync<T>(
        Func<CancellationToken, Task<T>> call,
        OffsideRefitOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (call is null)
            throw new ArgumentNullException(nameof(call));

        var resolved = options ?? _defaults;

        try
        {
            var value = await call(cancellationToken).ConfigureAwait(false);
            return Result<T>.Success(value);
        }
        catch (ApiException exception)
        {
            return exception.ToResult<T>(resolved);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            return Result<T>.Failure(RefitOffsideExtensions.Timeout(resolved, null, exception.Message));
        }
        catch (TimeoutException exception)
        {
            return Result<T>.Failure(RefitOffsideExtensions.Timeout(resolved, null, exception.Message));
        }
        catch (HttpRequestException exception)
        {
            return Result<T>.Failure(exception.ToOffsideError(resolved));
        }
    }

    public async Task<Result> CallAsync(
        Func<CancellationToken, Task> call,
        OffsideRefitOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (call is null)
            throw new ArgumentNullException(nameof(call));

        var result = await CallAsync<object?>(
            async token =>
            {
                await call(token).ConfigureAwait(false);
                return null;
            },
            options,
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Result.Success() : Result.Failure(result.Errors);
    }
}
