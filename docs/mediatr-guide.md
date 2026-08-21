# MediatR integration

*[Português](pt-BR/mediatr-guide.md) · [Back to docs](README.md)*

`Offside.MediatR` connects failed `Result` values to MediatR notifications without adding MediatR to the Offside core package. A notification always carries one `Error`; it is not a domain event describing a state change.

## Install and register

```bash
dotnet add package Offside
dotnet add package Offside.MediatR
```

Configure MediatR first, then the integration:

```csharp
using Offside.MediatR;

builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddOffsideMediatR();
```

`AddOffsideMediatR` is idempotent. It registers a scoped collector and its notification handler, but it does not call `AddMediatR`, register `IPublisher`, or configure a MediatR license.

You do not need to scan the `Offside.MediatR` assembly. If it is scanned, the collector protects a single notification instance from duplicate handler registration.

## Publish a result

Inject `IPublisher`, then publish at the application boundary:

```csharp
public async Task<Result> Cancel(string id, CancellationToken cancellationToken)
{
    var result = _orders.Cancel(id);
    return await result.PublishDomainNotificationsAsync(_publisher, cancellationToken);
}
```

The generic overload returns the same `Result<T>`:

```csharp
Result<Order> result = _orders.Get(id);
return await result.PublishDomainNotificationsAsync(_publisher, cancellationToken);
```

Success publishes nothing. Failure publishes one `DomainNotification` per error, sequentially and in result order. With `E` errors and `H` handlers, the upper-bound dispatch work is `E × H` handler executions.

## Read the collector

Inject `IDomainNotificationCollector` in the same dependency-injection scope:

```csharp
if (collector.HasNotifications)
    return collector.ToResult();

return collector.ToResult(order);
```

`Errors` returns an independent snapshot. `ToResult()` returns success when empty and a failure with the collected errors otherwise. `ToResult<T>(value)` returns the supplied value only when the collector is empty.

Reads never remove notifications; there is no `Clear`. Create one scope per logical operation:

```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var worker = scope.ServiceProvider.GetRequiredService<OrderWorker>();
await worker.Process(message, stoppingToken);
```

ASP.NET Core already creates one scope per request. Workers must create a new scope per message or job; reusing a long-lived scope also retains old errors.

## Ordering, concurrency, and failures

- The Result extensions guarantee error order because they await each publish before starting the next.
- Concurrent external publishes are thread-safe, but their relative order is unspecified.
- Handler exceptions and cancellation stop publication and propagate to the caller.
- Handlers for earlier errors — and handlers earlier in the current MediatR strategy — may already have run. There is no rollback.
- Retrying the extension creates new notifications and can repeat side effects. Make handlers idempotent when retries are possible.

Every handler receives the complete `Error`, including `Arguments` and diagnostic data on `Unexpected`. HTTP sanitization does not run in this path. Never place secrets in an error.

## MediatR versions and licensing

The package supports MediatR `12.0.1` through `14.x` and is tested against `12.0.1`, `13.1.0`, and `14.2.0`. The NuGet dependency range is `[12.0.1,15.0.0)`.

MediatR 13 introduced a license key and changed its upstream licensing model. Hosts using 13 or 14 must configure logging and evaluate the applicable upstream license. Configure a key through MediatR itself; Offside does not accept a key or suppress license warnings. See the [MediatR 13 release](https://github.com/LuckyPennySoftware/MediatR/releases) and [official licensing page](https://mediatr.io/).

## Deliberate omissions

- No automatic pipeline behavior: publication is visible at the call site.
- No `Clear`: the scope owns the collector lifetime.
- No retry or transaction abstraction: those policies belong to the application.
