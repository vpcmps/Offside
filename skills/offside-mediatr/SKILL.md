---
name: offside-mediatr
description: Publishes Offside Result errors as MediatR domain notifications and reads the scoped collector. Use when a .NET project combines Offside with MediatR, IPublisher, DomainNotification, or AddOffsideMediatR.
---

# Offside MediatR

Use this integration only in projects that already use MediatR. `Result` remains the primary failure contract.

## Register

```bash
dotnet add package Offside
dotnet add package Offside.MediatR
```

Configure MediatR in the host, then register Offside:

```csharp
using Offside.MediatR;

builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddOffsideMediatR();
```

`AddOffsideMediatR` is repeat-safe. It does not call `AddMediatR` or register `IPublisher`.

For MediatR 13+, configure logging and any required upstream license key in `AddMediatR`; Offside never configures or suppresses licensing.

## Publish failures

```csharp
var result = _orders.Cancel(id);
return await result.PublishDomainNotificationsAsync(_publisher, cancellationToken);
```

Success publishes nothing. A failure publishes one `DomainNotification` per `Error`, sequentially and in result order, then returns the original result.

Handler exceptions and cancellation stop the remaining errors and propagate. Earlier handlers may already have run; retrying can publish duplicates.

## Read collected errors

Inject `IDomainNotificationCollector` in the same DI scope:

```csharp
var result = collector.ToResult();
var valueResult = collector.ToResult(value);
```

`Errors` is a snapshot. Reads persist until the scope is disposed; there is no `Clear`. Create one scope per HTTP request, message, job, or other operation. Never reuse a worker scope across messages.

Concurrent publications are safe, but only the sequential Result extensions guarantee relative order.

## Do not

- Treat `DomainNotification` as a domain event; it carries one Offside error.
- Add an automatic pipeline behavior or throw for expected business failures.
- Put secrets in `Error.Arguments` or diagnostic details: every notification handler receives the complete `Error`.
